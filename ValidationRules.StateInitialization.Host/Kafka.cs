﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Transactions;

using Confluent.Kafka;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

using Newtonsoft.Json;

using NuClear.Messaging.Transports.Kafka;
using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.Commands;
using NuClear.Replication.Core.DataObjects;
using NuClear.Settings;
using NuClear.Settings.API;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Factories;
using NuClear.StateInitialization.Core.Storage;
using NuClear.Storage.API.ConnectionStrings;
using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Dto;
using NuClear.ValidationRules.Storage.Identitites.Connections;
using NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.StateInitialization.Host
{
    internal sealed class KafkaReplicationCommand : ICommand
    {
        public ReplicateInBulkCommand ReplicateInBulkCommand { get; }

        public KafkaReplicationCommand(ReplicateInBulkCommand replicateInBulkCommand)
        {
            ReplicateInBulkCommand = replicateInBulkCommand;
        }
    }

    internal sealed class KafkaReplicationActor : IActor
    {
        private readonly IDataObjectTypesProviderFactory _dataObjectTypesProviderFactory;
        private readonly IConnectionStringSettings _connectionStringSettings;
        private readonly IAccessorTypesProvider _accessorTypesProvider;
        private readonly ReceiverSettings _receiverSettings;

        public KafkaReplicationActor(
            IDataObjectTypesProviderFactory dataObjectTypesProviderFactory,
            IConnectionStringSettings connectionStringSettings)
        {
            _dataObjectTypesProviderFactory = dataObjectTypesProviderFactory;
            _connectionStringSettings = connectionStringSettings;
            _receiverSettings = new ReceiverSettings(connectionStringSettings);
            _accessorTypesProvider = new AccessorTypesProvider();
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var kafkaCommand in commands.OfType<KafkaReplicationCommand>())
            {
                var command = kafkaCommand.ReplicateInBulkCommand;
                var dataObjectTypes = GetDataObjectTypes(command);

                ExecuteInTransactionScope(command,
                dataConnection =>
                {
                    var bulkCopyOptions = new BulkCopyOptions { BulkCopyTimeout = (int)command.BulkCopyTimeout.TotalSeconds };
                    var actors = CreateActors(dataObjectTypes, dataConnection, bulkCopyOptions);

                    var receiver = new KafkaMessageFlowReceiver(_receiverSettings);

                    IReadOnlyCollection<Message> batch;
                    while((batch = receiver.ReceiveBatch(_receiverSettings.BatchSize)).Count != 0)
                    {
                        var replaceDataObjectCommands = batch.Aggregate(new List<ICommand>(), (list, x) =>
                        {
                            // пока что хардкод для advertisement
                            var dto = KafkaDeserializer.Json.Deserialize<AdvertisementDto>(x);

                            list.Add(new ReplaceDataObjectCommand(typeof(Advertisement), dto));
                            list.Add(new ReplaceDataObjectCommand(typeof(EntityName), dto));

                            return list;
                        }).ToList();

                        foreach (var actor in actors)
                        {
                            actor.ExecuteCommands(replaceDataObjectCommands);
                        }
                    }
                });
            }

            return Array.Empty<IEvent>();

            IReadOnlyCollection<Type> GetDataObjectTypes(ReplicateInBulkCommand command)
            {
                var dataObjectTypesProvider = (DataObjectTypesProvider)_dataObjectTypesProviderFactory.Create(command);
                return dataObjectTypesProvider.DataObjectTypes;
            }
        }

        private IReadOnlyCollection<IActor> CreateActors(IReadOnlyCollection<Type> dataObjectTypes, DataConnection dataConnection, BulkCopyOptions bulkCopyOptions)
        {
            var actors = new List<IActor>();

            foreach (var dataObjectType in dataObjectTypes)
            {
                var accessorTypes = _accessorTypesProvider.GetAccessorsFor(dataObjectType);
                foreach (var accessorType in accessorTypes)
                {
                    var accessor = Activator.CreateInstance(accessorType, (IQuery)null);
                    var actorType = typeof(BulkInsertDataObjectsActor<>).MakeGenericType(dataObjectType);
                    var actor = (IActor)Activator.CreateInstance(actorType, accessor, dataConnection, bulkCopyOptions);

                    actors.Add(actor);
                }
            }

            return actors;
        }

        #region copy-paste from StateInitialization.Core

        private static readonly TransactionOptions TransactionOptions =
            new TransactionOptions
                {
                    IsolationLevel = System.Transactions.IsolationLevel.Serializable,
                    Timeout = TimeSpan.Zero
                };

        private void ExecuteInTransactionScope(ReplicateInBulkCommand command, Action<DataConnection> action)
        {
            using (var transation = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionOptions))
            {
                using (var targetConnection = CreateDataConnection(command.TargetStorageDescriptor))
                {
                    action(targetConnection);
                    transation.Complete();
                }
            }
        }

        private DataConnection CreateDataConnection(StorageDescriptor storageDescriptor)
        {
            var connectionString = _connectionStringSettings.GetConnectionString(storageDescriptor.ConnectionStringIdentity);
            var connection = SqlServerTools.CreateDataConnection(connectionString);
            connection.AddMappingSchema(storageDescriptor.MappingSchema);
            connection.CommandTimeout = (int)storageDescriptor.CommandTimeout.TotalMilliseconds;
            return connection;
        }

        #endregion

        #region smart copy-paste from StateInitialization.Core

        private sealed class AccessorTypesProvider : IAccessorTypesProvider
        {
            private static readonly Lazy<IReadOnlyDictionary<Type, Type[]>> AccessorTypes = new Lazy<IReadOnlyDictionary<Type, Type[]>>(LoadAccessorTypes);

            private static IReadOnlyDictionary<Type, Type[]> LoadAccessorTypes()
                => AppDomain.CurrentDomain.GetAssemblies()
                            .Where(x => !x.IsDynamic)
                            .SelectMany(SafeGetAssemblyExportedTypes)
                            .SelectMany(type => type.GetInterfaces(), (type, @interface) => new { type, @interface })
                            .Where(x => !x.type.IsAbstract && x.@interface.IsGenericType && x.@interface.GetGenericTypeDefinition() == typeof(IMemoryBasedDataObjectAccessor<>))
                            .Select(x => new { GenericArgument = x.@interface.GetGenericArguments()[0], Type = x.type })
                            .GroupBy(x => x.GenericArgument, x => x.Type)
                            .ToDictionary(x => x.Key, x => x.ToArray());

            private static IEnumerable<Type> SafeGetAssemblyExportedTypes(Assembly assembly)
            {
                try
                {
                    return assembly.ExportedTypes;
                }
                catch
                {
                    return Array.Empty<Type>();
                }
            }

            public IReadOnlyCollection<Type> GetAccessorsFor(Type dataObjectType)
            {
                Type[] result;
                return AccessorTypes.Value.TryGetValue(dataObjectType, out result) ? result : Array.Empty<Type>(); ;
            }
        }

        // BulkInsertDataObjectsActor для IMemoryBasedDataObjectAccessor
        private sealed class BulkInsertDataObjectsActor<TDataObject> : IActor
            where TDataObject : class
        {
            private static readonly Type DataObjectType = typeof(TDataObject);

            private readonly IMemoryBasedDataObjectAccessor<TDataObject> _dataObjectAccessor;
            private readonly BulkCopyOptions _bulkCopyOptions;
            private readonly ITable<TDataObject> _table;

            public BulkInsertDataObjectsActor(IMemoryBasedDataObjectAccessor<TDataObject> dataObjectAccessor, DataConnection dataConnection, BulkCopyOptions bulkCopyOptions)
            {
                _dataObjectAccessor = dataObjectAccessor;
                _bulkCopyOptions = bulkCopyOptions;
                _table = dataConnection.GetTable<TDataObject>();
            }

            public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
            {
                var effectiveCommands = commands.OfType<IReplaceDataObjectCommand>().Where(x => x.DataObjectType == DataObjectType).ToList();
                if (effectiveCommands.Count != 0)
                {
                    var dataObjects = effectiveCommands.SelectMany(x => _dataObjectAccessor.GetDataObjects(x));

                    ExecuteBulkCopy(dataObjects);
                }

                return Array.Empty<IEvent>();
            }

            private void ExecuteBulkCopy(IEnumerable<TDataObject> source)
            {
                try
                {
                    _table.BulkCopy(_bulkCopyOptions, source);
                }
                catch (Exception ex)
                {
                    throw new DataException($"Error occured while bulk replacing data for dataobject of type {typeof(TDataObject).Name} using {_dataObjectAccessor.GetType().Name}{Environment.NewLine}", ex);
                }
            }
        }

        #endregion

        private sealed class ReceiverSettings : IKafkaMessageFlowReceiverSettings
        {
            private readonly StringSetting _amsFactsTopics = ConfigFileSetting.String.Required("AmsFactsTopics");
            private readonly StringSetting _pollTimeout = ConfigFileSetting.String.Optional("AmsPollTimeout", "00:00:05");
            private readonly IntSetting _batchSize = ConfigFileSetting.Int.Optional("AmsBatchSize", 500);

            public ReceiverSettings(IConnectionStringSettings connectionStringSettings)
            {
                var connectionString = connectionStringSettings.GetConnectionString(AmsConnectionStringIdentity.Instance);
                Config = JsonConvert.DeserializeObject<Dictionary<string, object>>(connectionString);
            }

            public string ClientId { get; } = "ValidationRules.StateInitialization.Host";
            public string GroupId { get; } = "ValidationRules.StateInitialization.Host";
            public Dictionary<string, object> Config { get; }

            public IEnumerable<string> Topics => _amsFactsTopics.Value.Split(',');
            public TimeSpan PollTimeout => TimeSpan.Parse(_pollTimeout.Value, CultureInfo.InvariantCulture);

            public Offset Offset { get; } = Offset.Beginning;
            public int BatchSize => _batchSize.Value;
        }

        private static class KafkaDeserializer
        {
            public static class Json
            {
                private static readonly JsonSerializer JsonSerializer = JsonSerializer.Create();

                public static T Deserialize<T>(Message message)
                {
                    using (var stream = new MemoryStream(message.Value))
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        return JsonSerializer.Deserialize<T>(jsonReader);
                    }
                }
            }
        }

        // CommandRegardlessDataObjectTypesProvider - он internal в StateInitiallization.Core, пришлось запилить вот это
        internal sealed class DataObjectTypesProvider : IDataObjectTypesProvider
        {
            public IReadOnlyCollection<Type> DataObjectTypes { get; }

            public DataObjectTypesProvider(IReadOnlyCollection<Type> dataObjectTypes)
            {
                DataObjectTypes = dataObjectTypes;
            }

            public IReadOnlyCollection<Type> Get<TCommand>() where TCommand : ICommand
            {
                throw new NotImplementedException();
            }
        }
    }
}
