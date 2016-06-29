﻿using System;
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
using NuClear.StateInitialization.Core.Actors;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.DataObjects;
using NuClear.StateInitialization.Core.Events;
using NuClear.StateInitialization.Core.Factories;
using NuClear.StateInitialization.Core.Settings;
using NuClear.StateInitialization.Core.Storage;
using NuClear.Storage.API.ConnectionStrings;

using IsolationLevel = System.Transactions.IsolationLevel;

namespace NuClear.StateInitialization.Core
{
    public sealed class BulkReplicationActor : IActor
    {
        private static readonly TransactionOptions TransactionOptions =
            new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.Serializable,
                    Timeout = TimeSpan.Zero
                };

        private readonly IDataObjectTypesProviderFactory _dataObjectTypesProviderFactory;
        private readonly IConnectionStringSettings _connectionStringSettings;
        private readonly IDbSchemaManagementSettings _schemaManagementSettings;

        public BulkReplicationActor(
            IDataObjectTypesProviderFactory dataObjectTypesProviderFactory,
            IConnectionStringSettings connectionStringSettings,
            IDbSchemaManagementSettings schemaManagementSettings)
        {
            _dataObjectTypesProviderFactory = dataObjectTypesProviderFactory;
            _connectionStringSettings = connectionStringSettings;
            _schemaManagementSettings = schemaManagementSettings;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var command in commands.Cast<ReplaceDataObjectsInBulkCommand>())
            {
                var commandStopwatch = Stopwatch.StartNew();

                var dataObjectTypes = GetDataObjectTypes(_dataObjectTypesProviderFactory.Create(command));

                var transation = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionOptions);
                var targetConnection = CreateDataConnection(command.TargetStorageDescriptor);
                try
                {
                    var schemaManagenentActor = CreateDbSchemaManagementActor((SqlConnection)targetConnection.Connection);
                    var schemaChangedEvents = schemaManagenentActor.ExecuteCommands(new ICommand[] { new DropViewsCommand(), new DisableContraintsCommand() });

                    Parallel.ForEach(dataObjectTypes, dataObjectType => ReplaceInBulk(command, dataObjectType, targetConnection));

                    var compensationalCommands = CreateCompensationalCommands(schemaChangedEvents);
                    if (compensationalCommands.Any())
                    {
                        schemaManagenentActor.ExecuteCommands(compensationalCommands);
                    }

                    transation.Complete();
                }
                finally
                {
                    targetConnection.Dispose();
                    transation.Dispose();
                }

                commandStopwatch.Stop();
                Console.WriteLine($"[{command.SourceStorageDescriptor.ConnectionStringIdentity}] -> [{command.TargetStorageDescriptor.ConnectionStringIdentity}]: {commandStopwatch.Elapsed.TotalSeconds} seconds");
            }

            return Array.Empty<IEvent>();
        }

        private static IEnumerable<Type> GetDataObjectTypes(IDataObjectTypesProvider dataObjectTypesProvider)
        {
            var commandRegardlessProvider = dataObjectTypesProvider as ICommandRegardlessDataObjectTypesProvider;
            if (commandRegardlessProvider == null)
            {
                throw new ApplicationException($"Data object types provider created with current instance of {typeof(IDataObjectTypesProviderFactory).Name} " +
                                               $"does not implement {typeof(ICommandRegardlessDataObjectTypesProvider).Name} interface");
            }

            return commandRegardlessProvider.Get();
        }

        private static IReadOnlyCollection<ICommand> CreateCompensationalCommands(IReadOnlyCollection<IEvent> events)
        {
            var commands = new List<ICommand>();

            var constraintsDisabledEvent = events.OfType<ConstraintsDisabledEvent>().SingleOrDefault();
            if (constraintsDisabledEvent != null)
            {
                commands.Add(new EnableConstraintsCommand(constraintsDisabledEvent.Checks, constraintsDisabledEvent.ForeignKeys));
            }

            var viewsDroppedEvent = events.OfType<ViewsDroppedEvent>().SingleOrDefault();
            if (viewsDroppedEvent != null)
            {
                commands.Add(new RestoreViewsCommand(viewsDroppedEvent.ViewsToRestore));
            }

            return commands;
        }

        private DataConnection CreateDataConnection(StorageDescriptor storageDescriptor)
        {
            var connectionString = _connectionStringSettings.GetConnectionString(storageDescriptor.ConnectionStringIdentity);
            var connection = SqlServerTools.CreateDataConnection(connectionString);
            connection.AddMappingSchema(storageDescriptor.MappingSchema);
            connection.CommandTimeout = (int)TimeSpan.FromMinutes(30).TotalMilliseconds;
            return connection;
        }

        private IActor CreateDbSchemaManagementActor(SqlConnection sqlConnection)
        {
            return new CompositeActor(
                new IActor[]
                    {
                        new ViewManagementActor(sqlConnection, _schemaManagementSettings),
                        new ConstraintsManagementActor(sqlConnection, _schemaManagementSettings)
                    });
        }

        private void ReplaceInBulk(ReplaceDataObjectsInBulkCommand command, Type dataObjectType, DataConnection targetConnection)
        {
            var commands = new ICommand[]
                       {
                           command,
                           new UpdateTableStatisticsCommand(command.TargetStorageDescriptor.MappingSchema)
                       };

            DataConnection sourceConnection;
            using (var suppressed = new TransactionScope(TransactionScopeOption.Suppress))
            {
                sourceConnection = CreateDataConnection(command.SourceStorageDescriptor);
                if (sourceConnection.Connection.State != ConnectionState.Open)
                {
                    sourceConnection.Connection.Open();
                }

                suppressed.Complete();
            }

            using (sourceConnection)
            {
                var actorsFactory = new ReplaceDataObjectsInBulkActorFactory(dataObjectType, sourceConnection, targetConnection);
                foreach (var actor in actorsFactory.Create())
                {
                    var sw = Stopwatch.StartNew();
                    actor.ExecuteCommands(commands);
                    sw.Stop();

                    Console.WriteLine($"{actor.GetType().GetFriendlyName()}: {sw.Elapsed.TotalSeconds} seconds");
                }
            }
        }
    }
}