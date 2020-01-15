using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Confluent.Kafka;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

using Microsoft.SqlServer.Management.Smo;

using NuClear.Messaging.API.Flows;
using NuClear.Messaging.Transports.Kafka;
using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Factories;
using NuClear.StateInitialization.Core.Storage;
using NuClear.Storage.API.ConnectionStrings;
using NuClear.Storage.API.Readings;
using NuClear.Tracing.API;
using NuClear.ValidationRules.Hosting.Common;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace NuClear.ValidationRules.StateInitialization.Host.Kafka
{
    internal sealed class KafkaReplicationActor : IActor
    {
        private readonly IConnectionStringSettings _connectionStringSettings;
        private readonly IKafkaMessageFlowReceiverFactory _receiverFactory;
        private readonly KafkaMessageFlowInfoProvider _kafkaMessageFlowInfoProvider;
        private readonly IReadOnlyCollection<IBulkCommandFactory<ConsumeResult<Ignore, byte[]>>> _commandFactories;
        private readonly ITracer _tracer;

        private readonly IAccessorTypesProvider _accessorTypesProvider = new InMemoryAccessorTypesProvider();

        public KafkaReplicationActor(
            IConnectionStringSettings connectionStringSettings,
            IKafkaMessageFlowReceiverFactory kafkaMessageFlowReceiverFactory,
            KafkaMessageFlowInfoProvider kafkaMessageFlowInfoProvider,
            IReadOnlyCollection<IBulkCommandFactory<ConsumeResult<Ignore, byte[]>>> commandFactories,
            ITracer tracer)
        {
            _connectionStringSettings = connectionStringSettings;
            _receiverFactory = kafkaMessageFlowReceiverFactory;
            _kafkaMessageFlowInfoProvider = kafkaMessageFlowInfoProvider;
            _commandFactories = commandFactories;
            _tracer = tracer;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var kafkaCommand in commands.OfType<KafkaReplicationCommand>())
            {
                var dataObjectTypes = kafkaCommand.ReplicateInBulkCommand.TypesToReplicate;

                using var targetConnection = CreateDataConnection(kafkaCommand.ReplicateInBulkCommand.TargetStorageDescriptor);
                
                LoadDataFromKafka2Db(kafkaCommand.MessageFlows,
                    dataObjectTypes,
                    targetConnection,
                    kafkaCommand.BatchSize,
                    (int)kafkaCommand.ReplicateInBulkCommand.BulkCopyTimeout.TotalSeconds);

                if (!kafkaCommand.ReplicateInBulkCommand.DbManagementMode.HasFlag(DbManagementMode.UpdateTableStatistics))
                {
                    continue;
                }

                IReadOnlyCollection<ICommand> updateStatisticsCommands =
                    dataObjectTypes.Select(t => kafkaCommand.ReplicateInBulkCommand.TargetStorageDescriptor.MappingSchema.GetTableName(t))
                        .Select(table => new UpdateTableStatisticsActor.UpdateTableStatisticsCommand(table,
                            StatisticsTarget.All,
                            StatisticsScanType.FullScan))
                        .ToList();
                var updateStatisticsActor = new UpdateTableStatisticsActor((SqlConnection)targetConnection.Connection);
                updateStatisticsActor.ExecuteCommands(updateStatisticsCommands);
            }

            return Array.Empty<IEvent>();
        }

        private void LoadDataFromKafka2Db(IMessageFlow[] messageFlows,
                                          IReadOnlyCollection<Type> dataObjectTypes,
                                          DataConnection dataConnection,
                                          int batchSize,
                                          int bulkReplaceCommandTimeoutSec)
        {
            var actors = CreateActors(dataObjectTypes,
                                      dataConnection,
                                      new BulkCopyOptions
                                      {
                                          BulkCopyTimeout = bulkReplaceCommandTimeoutSec
                                      });

            var initialStats = _kafkaMessageFlowInfoProvider.GetFlowStats(messageFlows).ToDictionary(x => x.TopicPartition);
            
            using var receiver = _receiverFactory.Create(messageFlows);

            while(true)
            {
                var batch = receiver.ReceiveBatch(batchSize);
                if (batch.Count == 0)
                {
                    break;
                }
                
                var bulkCommands = _commandFactories.SelectMany(factory => factory.CreateCommands(batch)).ToList();
                if (bulkCommands.Count > 0)
                {
                    
                    using var scope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable, Timeout = TimeSpan.Zero });
                    foreach (var actor in actors)
                    {
                        actor.ExecuteCommands(bulkCommands);
                    }
                    scope.Complete();
                }

                receiver.CompleteBatch(batch);
                
                var stats = _kafkaMessageFlowInfoProvider.GetFlowStats(messageFlows);
                foreach (var stat in stats)
                {
                    _tracer.Info($"Topic {stat.TopicPartition}, End: {stat.End}, Offset: {stat.Offset}, Lag: {stat.Lag}");
                }

                if (stats.All(x => initialStats[x.TopicPartition].End <= x.Offset))
                {
                    break;
                }
            }
        }

        private IReadOnlyCollection<IActor> CreateActors(IReadOnlyCollection<Type> dataObjectTypes,
                                                         DataConnection dataConnection,
                                                         BulkCopyOptions bulkCopyOptions)
        {
            var actors = new List<IActor>();

            foreach (var dataObjectType in dataObjectTypes)
            {
                var accessorTypes = _accessorTypesProvider.GetAccessorsFor(dataObjectType);
                foreach (var accessorType in accessorTypes)
                {
                    var accessor = Activator.CreateInstance(accessorType, (IQuery)null);
                    var actorType = typeof(BulkInsertInMemoryDataObjectsActor<>).MakeGenericType(dataObjectType);
                    var actor = (IActor)Activator.CreateInstance(actorType, accessor, dataConnection, bulkCopyOptions);

                    actors.Add(actor);
                }
            }

            return actors;
        }

        private DataConnection CreateDataConnection(StorageDescriptor storageDescriptor)
        {
            var connectionString = _connectionStringSettings.GetConnectionString(storageDescriptor.ConnectionStringIdentity);
            var connection = SqlServerTools.CreateDataConnection(connectionString);
            connection.AddMappingSchema(storageDescriptor.MappingSchema);
            connection.CommandTimeout = (int)storageDescriptor.CommandTimeout.TotalMilliseconds;
            return connection;
        }
    }
}
