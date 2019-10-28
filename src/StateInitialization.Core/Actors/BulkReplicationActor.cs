using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.DataObjects;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Events;
using NuClear.StateInitialization.Core.Factories;
using NuClear.StateInitialization.Core.Storage;
using NuClear.Storage.API.ConnectionStrings;

using IsolationLevel = System.Transactions.IsolationLevel;

namespace NuClear.StateInitialization.Core.Actors
{
    // ReSharper disable once UnusedMember.Global
    public sealed class BulkReplicationActor : IActor
    {
        private static readonly TransactionOptions TransactionOptions =
            new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Serializable,
                Timeout = TimeSpan.Zero
            };

        private readonly IDataObjectTypesProvider _dataObjectTypesProvider;
        private readonly IConnectionStringSettings _connectionStringSettings;
        private readonly BulkReplicator _bulkReplicator;
        private readonly FlowReplicator _flowReplicator;

        public BulkReplicationActor(
            IDataObjectTypesProvider dataObjectTypesProvider,
            IConnectionStringSettings connectionStringSettings)
            : this(dataObjectTypesProvider, connectionStringSettings, new StaticAccessorTypesProvider())
        {
        }

        public BulkReplicationActor(
            IDataObjectTypesProvider dataObjectTypesProvider,
            IConnectionStringSettings connectionStringSettings,
            IAccessorTypesProvider accessorTypesProvider)
        {
            _dataObjectTypesProvider = dataObjectTypesProvider;
            _connectionStringSettings = connectionStringSettings;
            _bulkReplicator = new BulkReplicator(accessorTypesProvider);
            _flowReplicator = new FlowReplicator(accessorTypesProvider);
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var command in commands)
            {
                var commandStopwatch = Stopwatch.StartNew();
                switch (command)
                {
                    case ReplicateInBulkCommand replicateInBulkCommand:
                        ExecuteCommand(replicateInBulkCommand);
                        break;
                    case InitializeFromFlowCommand initializeFromBusCommand: //
                        ExecuteCommand(initializeFromBusCommand);
                        break;
                }

                commandStopwatch.Stop();
                Console.WriteLine($"{command}: {commandStopwatch.Elapsed.TotalSeconds:F3} seconds");
            }

            return Array.Empty<IEvent>();
        }

        private void ExecuteCommand(InitializeFromFlowCommand command)
        {
            var dataObjectTypes = _dataObjectTypesProvider.Get(command);

            var tableTypesDictionary = dataObjectTypes
                .GroupBy(t => command.TargetStorageDescriptor.MappingSchema.GetTableName(t))
                .ToDictionary(g => g.Key, g => g.ToArray());

            ExecuteInTransactionScope(
                command.TargetStorageDescriptor,
                (targetConnection, schemaManagenentActor) =>
                {
                    var schemaChangedEvents = schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCommands(command.DbManagementMode));
                    var replicationCommands = CreateFlowProcessingCommands(tableTypesDictionary.Keys, command.BulkCopyTimeout, command.DbManagementMode);
                    var flow = command.FlowFactory.Invoke(_connectionStringSettings);
                    _flowReplicator.Replicate(dataObjectTypes, flow, targetConnection, replicationCommands);
                    schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCompensationalCommands(schemaChangedEvents));
                });
        }

        private void ExecuteCommand(ReplicateInBulkCommand command)
        {
            var dataObjectTypes = _dataObjectTypesProvider.Get(command);

            var tableTypesDictionary = dataObjectTypes
                .GroupBy(t => command.TargetStorageDescriptor.MappingSchema.GetTableName(t))
                .ToDictionary(g => g.Key, g => g.ToArray());

            var executionStrategy = DetermineExecutionStrategy(command);
            executionStrategy.Invoke(command, tableTypesDictionary);
        }

        private static SequentialPipelineActor CreateDbSchemaManagementActor(SqlConnection sqlConnection, TimeSpan commandTimeout)
        {
            return new SequentialPipelineActor(
                new IActor[]
                    {
                        new ViewManagementActor(sqlConnection, commandTimeout),
                        new ReplaceTableActor(sqlConnection),
                        new ConstraintsManagementActor(sqlConnection, commandTimeout)
                    });
        }

        private static IReadOnlyCollection<ICommand> CreateSchemaChangesCommands(DbManagementMode mode)
        {
            var commands = new List<ICommand>();
            if (mode.HasFlag(DbManagementMode.DropAndRecreateViews))
            {
                commands.Add(new DropViewsCommand());
            }

            if (mode.HasFlag(DbManagementMode.DropAndRecreateConstraints))
            {
                commands.Add(new DisableConstraintsCommand());
            }

            return commands;
        }

        private static IReadOnlyCollection<ICommand> CreateSchemaChangesCompensationalCommands(IReadOnlyCollection<IEvent> events)
        {
            var commands = new List<ICommand>();

            var constraintsDisabledEvent = events.OfType<ConstraintsDisabledEvent>().SingleOrDefault();
            if (constraintsDisabledEvent != null)
            {
                commands.Add(new EnableConstraintsCommand(constraintsDisabledEvent.Checks, constraintsDisabledEvent.Defaults, constraintsDisabledEvent.ForeignKeys));
            }

            var viewsDroppedEvent = events.OfType<ViewsDroppedEvent>().SingleOrDefault();
            if (viewsDroppedEvent != null)
            {
                commands.Add(new RestoreViewsCommand(viewsDroppedEvent.ViewsToRestore));
            }

            return commands;
        }

        private static IReadOnlyCollection<ICommand> CreateFlowProcessingCommands(IReadOnlyCollection<TableName> tables, TimeSpan bulkCopyTimeout, DbManagementMode mode)
        {
            var commands = new List<ICommand>();

            commands.AddRange(tables.SelectMany(x => CreatePreReplicationCommands(x, mode)));
            commands.Add(new BulkInsertDataObjectsCommand(bulkCopyTimeout));
            commands.AddRange(tables.SelectMany(x => CreatePostReplicationCommands(x, mode)));

            return commands;
        }

        private static IReadOnlyCollection<ICommand> CreateReplicationCommands(TableName table, TimeSpan bulkCopyTimeout, DbManagementMode mode)
        {
            var commands = new List<ICommand>();

            commands.AddRange(CreatePreReplicationCommands(table, mode));
            commands.Add(new BulkInsertDataObjectsCommand(bulkCopyTimeout));
            commands.AddRange(CreatePostReplicationCommands(table, mode));

            return commands;
        }

        private static IEnumerable<ICommand> CreatePreReplicationCommands(TableName table, DbManagementMode mode)
        {
            if (mode.HasFlag(DbManagementMode.EnableIndexManagment))
            {
                yield return new DisableIndexesCommand(table);
            }

            if (mode.HasFlag(DbManagementMode.TruncateTable))
            {
                yield return new TruncateTableCommand(table);
            }
        }

        private static IEnumerable<ICommand> CreatePostReplicationCommands(TableName table, DbManagementMode mode)
        {
            if (mode.HasFlag(DbManagementMode.EnableIndexManagment))
            {
                yield return new EnableIndexesCommand(table);
            }

            if (mode.HasFlag(DbManagementMode.UpdateTableStatistics))
            {
                yield return new UpdateTableStatisticsCommand(table);
            }
        }

        private static IReadOnlyCollection<ICommand> CreateShadowReplicationCommands(TableName table, TimeSpan bulkCopyTimeout, DbManagementMode mode)
        {
            var targetTable = ShadowCopyTableName(table);
            var commands = new List<ICommand>
                               {
                                   new CreateTableCopyCommand(table, targetTable),
                                   new DisableIndexesCommand(targetTable),
                                   new BulkInsertDataObjectsCommand(bulkCopyTimeout, targetTable),
                                   new EnableIndexesCommand(targetTable)
                               };

            if (mode.HasFlag(DbManagementMode.UpdateTableStatistics))
            {
                commands.Add(new UpdateTableStatisticsCommand(targetTable));
            }

            return commands;
        }

        private static IReadOnlyCollection<ICommand> CreateTablesReplacingCommands(IEnumerable<TableName> tables)
        {
            return tables
                .Select(t => new ReplaceTableCommand(t, ShadowCopyTableName(t)))
                .ToList();
        }

        private static TableName ShadowCopyTableName(TableName table)
            => new TableName("river_" + table.Table, table.Schema);

        private Action<ReplicateInBulkCommand, IReadOnlyDictionary<TableName, Type[]>> DetermineExecutionStrategy(ReplicateInBulkCommand command)
        {
            if (command.ExecutionMode == ExecutionMode.Sequential)
            {
                return SequentialExecutionStrategy;
            }

            if (command.ExecutionMode.Shadow)
            {
                return (cmd, types) => ShadowParallelExecutionStrategy(cmd, types, command.ExecutionMode.ParallelOptions);
            }

            return (cmd, types) => ParallelExecutionStrategy(cmd, types, command.ExecutionMode.ParallelOptions);
        }

        private void ShadowParallelExecutionStrategy(ReplicateInBulkCommand command, IReadOnlyDictionary<TableName, Type[]> tableTypesDictionary, ParallelOptions options)
        {
            Parallel.ForEach(
                    tableTypesDictionary,
                    options,
                    tableTypesPair =>
                    {
                        using (var targetConnection = CreateDataConnection(command.TargetStorageDescriptor))
                        using (var sourceConnection = CreateTransactionlessDataConnection(command.SourceStorageDescriptor))
                        {
                            try
                            {
                                var replicationCommands = CreateShadowReplicationCommands(tableTypesPair.Key, command.BulkCopyTimeout, command.DbManagementMode);
                                _bulkReplicator.Replicate(tableTypesPair.Value, sourceConnection, targetConnection, replicationCommands);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Failed to replicate using shadow parallel strategy {tableTypesPair.Key}", ex);
                            }
                        }
                    });

            ExecuteInTransactionScope(
                command.TargetStorageDescriptor,
                (targetConnection, schemaManagenentActor) =>
                {
                    var schemaChangedEvents = schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCommands(command.DbManagementMode));

                    // Delete existed tables then rename newly created ones (remove prefix):
                    schemaManagenentActor.ExecuteCommands(CreateTablesReplacingCommands(tableTypesDictionary.Keys));
                    schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCompensationalCommands(schemaChangedEvents));
                });
        }

        private void ParallelExecutionStrategy(ReplicateInBulkCommand command, IReadOnlyDictionary<TableName, Type[]> tableTypesDictionary, ParallelOptions options)
        {
            IReadOnlyCollection<IEvent> schemaChangedEvents = null;
            ExecuteInTransactionScope(
                command.TargetStorageDescriptor,
                (targetConnection, schemaManagenentActor) =>
                {
                    schemaChangedEvents = schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCommands(command.DbManagementMode));
                });

            Parallel.ForEach(
                    tableTypesDictionary,
                    options,
                    tableTypesPair =>
                    {
                        using (var targetConnection = CreateDataConnection(command.TargetStorageDescriptor))
                        using (var sourceConnection = CreateTransactionlessDataConnection(command.SourceStorageDescriptor))
                        {
                            try
                            {
                                var replicationCommands = CreateReplicationCommands(tableTypesPair.Key, command.BulkCopyTimeout, command.DbManagementMode);
                                _bulkReplicator.Replicate(tableTypesPair.Value, sourceConnection, targetConnection, replicationCommands);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Failed to replicate using parallel strategy {tableTypesPair.Key}", ex);
                            }
                        }
                    });

            ExecuteInTransactionScope(
                command.TargetStorageDescriptor,
                (targetConnection, schemaManagenentActor) =>
                    {
                        schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCompensationalCommands(schemaChangedEvents));
                    });
        }

        private void SequentialExecutionStrategy(ReplicateInBulkCommand command, IReadOnlyDictionary<TableName, Type[]> tableTypesDictionary)
        {
            ExecuteInTransactionScope(
                command.TargetStorageDescriptor,
                (targetConnection, schemaManagenentActor) =>
                {
                    var schemaChangedEvents = schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCommands(command.DbManagementMode));
                    using (var sourceConnection = CreateTransactionlessDataConnection(command.SourceStorageDescriptor))
                    {
                        foreach (var tableTypesPair in tableTypesDictionary)
                        {
                            try
                            {
                                var replicationCommands = CreateReplicationCommands(tableTypesPair.Key, command.BulkCopyTimeout, command.DbManagementMode);
                                _bulkReplicator.Replicate(tableTypesPair.Value, sourceConnection, targetConnection, replicationCommands);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Failed to replicate using sequential strategy {tableTypesPair.Key}", ex);
                            }
                        }
                    }

                    schemaManagenentActor.ExecuteCommands(CreateSchemaChangesCompensationalCommands(schemaChangedEvents));
                });
        }

        private void ExecuteInTransactionScope(StorageDescriptor storageDescriptor, Action<DataConnection, SequentialPipelineActor> action)
        {
            using (var transation = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionOptions))
            {
                using (var targetConnection = CreateDataConnection(storageDescriptor))
                {
                    var schemaManagenentActor = CreateDbSchemaManagementActor((SqlConnection)targetConnection.Connection, storageDescriptor.CommandTimeout);
                    action(targetConnection, schemaManagenentActor);
                    transation.Complete();
                }
            }
        }

        private DataConnection CreateTransactionlessDataConnection(StorageDescriptor storageDescriptor)
        {
            // Creating connection to source that will NOT be enlisted in transactions
            using (var scope = new TransactionScope(TransactionScopeOption.Suppress))
            {
               var connection = CreateDataConnection(storageDescriptor);
                if (connection.Connection.State != ConnectionState.Open)
                {
                    connection.Connection.Open();
                }

                scope.Complete();

                return connection;
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
    }
}
