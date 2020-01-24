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
using LinqToDB.Mapping;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.Tenancy;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Events;
using NuClear.StateInitialization.Core.Factories;
using NuClear.StateInitialization.Core.Storage;
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

        private readonly ITenantConnectionStringSettings _connectionStringSettings;
        private readonly BulkReplicator _bulkReplicator;

        public BulkReplicationActor(ITenantConnectionStringSettings connectionStringSettings)
            : this(connectionStringSettings, new StaticAccessorTypesProvider())
        {
        }

        public BulkReplicationActor(
            ITenantConnectionStringSettings connectionStringSettings,
            IAccessorTypesProvider accessorTypesProvider)
        {
            _connectionStringSettings = connectionStringSettings;
            _bulkReplicator = new BulkReplicator(accessorTypesProvider);
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var command in commands.OfType<ReplicateInBulkCommand>())
            {
                var commandStopwatch = Stopwatch.StartNew();
                ExecuteCommand(command);
                commandStopwatch.Stop();
                Console.WriteLine($"{command}: {commandStopwatch.Elapsed.TotalSeconds:F3} seconds");
            }

            return Array.Empty<IEvent>();
        }

        private void ExecuteCommand(ReplicateInBulkCommand command)
        {
            ParallelExecutionStrategy(command);
        }

        private static SequentialPipelineActor CreateDbSchemaManagementActor(SqlConnection sqlConnection,
            TimeSpan commandTimeout)
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

        private static IReadOnlyCollection<ICommand> CreateSchemaChangesCompensationalCommands(
            IReadOnlyCollection<IEvent> events)
        {
            var commands = new List<ICommand>();

            var constraintsDisabledEvent = events.OfType<ConstraintsDisabledEvent>().SingleOrDefault();
            if (constraintsDisabledEvent != null)
            {
                commands.Add(new EnableConstraintsCommand(constraintsDisabledEvent.Checks,
                    constraintsDisabledEvent.Defaults, constraintsDisabledEvent.ForeignKeys));
            }

            var viewsDroppedEvent = events.OfType<ViewsDroppedEvent>().SingleOrDefault();
            if (viewsDroppedEvent != null)
            {
                commands.Add(new RestoreViewsCommand(viewsDroppedEvent.ViewsToRestore));
            }

            return commands;
        }

        private static IReadOnlyCollection<ICommand> CreateReplicationCommands(
            TableName table,
            TimeSpan bulkCopyTimeout, 
            DbManagementMode mode,
            Tenant? tenant)
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

        private void ParallelExecutionStrategy(ReplicateInBulkCommand command)
        {
            var tableTypesDictionary = command.TypesToReplicate
                .ToLookup(x => command.TargetStorageDescriptor.MappingSchema.GetTableName(x));

            IReadOnlyCollection<IEvent> schemaChangedEvents = null;
            ExecuteInTransactionScope(
                command.TargetStorageDescriptor,
                (targetConnection, schemaManagementActor) =>
                {
                    schemaChangedEvents =
                        schemaManagementActor.ExecuteCommands(CreateSchemaChangesCommands(command.DbManagementMode));
                });

            Parallel.ForEach(
                tableTypesDictionary,
                command.ExecutionMode.ParallelOptions,
                tableTypesPair => Replicate(command, tableTypesPair.Key, tableTypesPair.ToList()));

            ExecuteInTransactionScope(
                command.TargetStorageDescriptor,
                (targetConnection, schemaManagenentActor) =>
                {
                    schemaManagenentActor.ExecuteCommands(
                        CreateSchemaChangesCompensationalCommands(schemaChangedEvents));
                });
        }

        private void Replicate(ReplicateInBulkCommand command, TableName tableName, IReadOnlyCollection<Type> types)
        {
            using var targetConnection = CreateDataConnection(command.TargetStorageDescriptor);
            using var sourceConnection = CreateSourceQuery(command.SourceStorageDescriptor);

            try
            {
                var replicationCommands = CreateReplicationCommands(
                    tableName,
                    command.BulkCopyTimeout,
                    command.DbManagementMode,
                    command.SourceStorageDescriptor.Tenant);

                Console.WriteLine($"Replicating {tableName}");
                _bulkReplicator.Replicate(
                    types,
                    sourceConnection,
                    targetConnection,
                    replicationCommands,
                    command.SourceStorageDescriptor.Tenant);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to replicate using parallel strategy {tableName}", ex);
            }
        }

        private void ExecuteInTransactionScope(StorageDescriptor storageDescriptor,
            Action<DataConnection, SequentialPipelineActor> action)
        {
            using (var transation = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionOptions))
            {
                using (var targetConnection = CreateDataConnection(storageDescriptor))
                {
                    var schemaManagenentActor =
                        CreateDbSchemaManagementActor((SqlConnection) targetConnection.Connection,
                            storageDescriptor.CommandTimeout);
                    action(targetConnection, schemaManagenentActor);
                    transation.Complete();
                }
            }
        }

        private DataConnection CreateSourceQuery(StorageDescriptor storageDescriptor)
        {
            var connectionString = storageDescriptor.Tenant.HasValue
                ? _connectionStringSettings.GetConnectionString(
                    storageDescriptor.ConnectionStringIdentity, storageDescriptor.Tenant.Value)
                : _connectionStringSettings.GetConnectionString(
                    storageDescriptor.ConnectionStringIdentity);

            if(string.IsNullOrWhiteSpace(connectionString))
                throw new Exception($"No connection string for {storageDescriptor.ConnectionStringIdentity.GetType().Name} for tenant {storageDescriptor.Tenant} is defined");

            using var scope = new TransactionScope(TransactionScopeOption.Suppress);

            // Creating connection to source that will NOT be enlisted in transactions
            var connection = CreateDataConnection(
                connectionString,
                storageDescriptor.MappingSchema,
                storageDescriptor.CommandTimeout);

            if (connection.Connection.State != ConnectionState.Open)
            {
                connection.Connection.Open();
            }

            scope.Complete();

            return connection;
        }

        private DataConnection CreateDataConnection(StorageDescriptor storageDescriptor)
            => CreateDataConnection(
                _connectionStringSettings.GetConnectionString(storageDescriptor.ConnectionStringIdentity),
                storageDescriptor.MappingSchema,
                storageDescriptor.CommandTimeout);

        private DataConnection CreateDataConnection(string connectionString, MappingSchema schema, TimeSpan timeout)
        {
            var connection = SqlServerTools.CreateDataConnection(connectionString);
            connection.AddMappingSchema(schema);
            connection.CommandTimeout = (int) timeout.TotalMilliseconds;
            return connection;
        }
    }
}