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
using NuClear.Replication.Core.DataObjects;
using NuClear.StateInitialization.Core;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Factories;
using NuClear.StateInitialization.Core.Storage;
using NuClear.Storage.API.ConnectionStrings;
using NuClear.Storage.API.Readings;
using NuClear.Tracing.API;
using NuClear.ValidationRules.Hosting.Common;
using Polly;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace NuClear.ValidationRules.StateInitialization.Host.Kafka
{
    internal sealed class KafkaReplicationActor : IActor
    {
        private readonly IConnectionStringSettings _connectionStringSettings;
        private readonly IDataObjectTypesProvider _dataObjectTypesProvider;
        private readonly IKafkaMessageFlowReceiverFactory _receiverFactory;
        private readonly KafkaMessageFlowInfoProvider _kafkaMessageFlowInfoProvider;
        private readonly IReadOnlyCollection<IBulkCommandFactory<ConsumeResult<Ignore, byte[]>>> _commandFactories;
        private readonly ITracer _tracer;

        private readonly IAccessorTypesProvider _accessorTypesProvider = new InMemoryAccessorTypesProvider();

        public KafkaReplicationActor(
            IConnectionStringSettings connectionStringSettings,
            IDataObjectTypesProvider dataObjectTypesProvider,
            IKafkaMessageFlowReceiverFactory kafkaMessageFlowReceiverFactory,
            KafkaMessageFlowInfoProvider kafkaMessageFlowInfoProvider,
            IReadOnlyCollection<IBulkCommandFactory<ConsumeResult<Ignore, byte[]>>> commandFactories,
            ITracer tracer)
        {
            _connectionStringSettings = connectionStringSettings;
            _dataObjectTypesProvider = dataObjectTypesProvider;
            _receiverFactory = kafkaMessageFlowReceiverFactory;
            _kafkaMessageFlowInfoProvider = kafkaMessageFlowInfoProvider;
            _commandFactories = commandFactories;
            _tracer = tracer;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var kafkaCommand in commands.OfType<KafkaReplicationCommand>())
            {
                var dataObjectTypes = _dataObjectTypesProvider.Get(kafkaCommand);

                using var targetConnection = CreateDataConnection(kafkaCommand.ReplicateInBulkCommand.TargetStorageDescriptor);
                
                LoadDataFromKafka2Db(kafkaCommand.MessageFlow,
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

        private void LoadDataFromKafka2Db(IMessageFlow messageFlowForKafkaTopic,
                                          IReadOnlyCollection<Type> dataObjectTypes,
                                          DataConnection dataConnection,
                                          int batchSize,
                                          int bulkReplaceCommandTimeoutSec)
        {
            var targetMessageFlowDescription = messageFlowForKafkaTopic.GetType().Name;

            var actors = CreateActors(dataObjectTypes,
                                      dataConnection,
                                      new BulkCopyOptions
                                      {
                                          BulkCopyTimeout = bulkReplaceCommandTimeoutSec
                                      });

            using var receiver = _receiverFactory.Create(messageFlowForKafkaTopic);
            // retry добавлен из-за https://github.com/confluentinc/confluent-kafka-dotnet/issues/86
            var lastTargetMessageOffset =
                Policy.Handle<KafkaException>(exception => exception.Error.Code == ErrorCode.LeaderNotAvailable)
                    .WaitAndRetryForever(i => TimeSpan.FromSeconds(5),
                        (exception, waitSpan) =>
                            _tracer.Warn(exception,
                                $"Can't get size of kafka topic. Message flow: {targetMessageFlowDescription}. Wait span: {waitSpan}"))
                    .ExecuteAndCapture(() => _kafkaMessageFlowInfoProvider.GetFlowSize(messageFlowForKafkaTopic) - 1)
                    .Result;

            _tracer.Info($"Receiving messages from kafka for flow: {targetMessageFlowDescription}. Last target message offset: {lastTargetMessageOffset}");

            var resolvedCommandFactories = _commandFactories.Where(f => f.AppropriateFlows.Contains(messageFlowForKafkaTopic))
                .ToList();

            for (var distance = lastTargetMessageOffset; distance > 0;)
            {
                var batch = receiver.ReceiveBatch(batchSize);

                var lastMessageOffset = batch.Last().Offset.Value;
                distance = lastTargetMessageOffset - lastMessageOffset;

                _tracer.Info($"Flow: {targetMessageFlowDescription}. Received messages: {batch.Count}. Last message offset for received batch: {lastMessageOffset}. Target and current offsets distance: {distance}");

                var bulkCommands = resolvedCommandFactories.SelectMany(factory => factory.CreateCommands(batch)).ToList();
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
            }

            _tracer.Info($"Receiving messages from kafka for flow: {targetMessageFlowDescription} finished");
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
