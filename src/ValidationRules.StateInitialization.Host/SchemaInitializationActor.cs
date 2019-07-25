using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Storage.API.ConnectionStrings;
using NuClear.ValidationRules.SingleCheck.Store;
using NuClear.ValidationRules.Storage;
using NuClear.ValidationRules.Storage.Connections;
using NuClear.ValidationRules.Storage.SchemaInitializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using ValidationRules.Hosting.Common;

namespace NuClear.ValidationRules.StateInitialization.Host
{
    public sealed class SchemaInitializationActor : IActor
    {
        private readonly IConnectionStringSettings _connectionStringSettings;

        public SchemaInitializationActor(IConnectionStringSettings connectionStringSettings)
        {
            _connectionStringSettings = connectionStringSettings;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var schemaInitializationCommands = commands.OfType<SchemaInitializationCommand>();
            foreach (var cmd in schemaInitializationCommands)
            {
                ExecuteCommand(cmd);
            }

            return Array.Empty<IEvent>();
        }

        private void ExecuteCommand(SchemaInitializationCommand cmd)
        {
            using (var db = CreateDataConnection(cmd))
            {
                var service = new SqlSchemaService(db);
                var allTables = service.AllTables();

                var tablesToDelete = allTables
                    .Where(x => cmd.SqlSchemas.Contains(x.SchemaName));

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    service.DropTables(tablesToDelete);
                    service.CreateTables(cmd.DataTypes);
                    scope.Complete();
                }
            }
        }

        private DataConnection CreateDataConnection(SchemaInitializationCommand command)
        {
            var connectionString = _connectionStringSettings.GetConnectionString(command.ConnectionStringIdentity);
            var connection = SqlServerTools.CreateDataConnection(connectionString);
            connection.AddMappingSchema(command.MappingSchema);
            return connection;
        }
    }

    public sealed class SchemaInitializationCommand : ICommand
    {
        public SchemaInitializationCommand(MappingSchema mappingSchema, IReadOnlyCollection<Type> dataTypes, IConnectionStringIdentity connectionStringIdentity, IReadOnlyCollection<string> sqlSchemas)
        {
            MappingSchema = mappingSchema;
            DataTypes = dataTypes;
            SqlSchemas = sqlSchemas;
            ConnectionStringIdentity = connectionStringIdentity;
        }

        public MappingSchema MappingSchema { get; }
        public IReadOnlyCollection<string> SqlSchemas { get; }
        public IReadOnlyCollection<Type> DataTypes { get; }
        public IConnectionStringIdentity ConnectionStringIdentity { get; }
    }

    public static class SchemaInitializationCommands
    {
        public static SchemaInitializationCommand Facts { get; }
            = new SchemaInitializationCommand(Schema.Facts,
                DataObjectTypesProviderFactory.AllSourcesFactTypes,
                ValidationRulesConnectionStringIdentity.Instance,
                new[] {"Facts"});

        public static SchemaInitializationCommand Aggregates { get; }
            = new SchemaInitializationCommand(Schema.Aggregates, DataObjectTypesProviderFactory.AggregateTypes,
                ValidationRulesConnectionStringIdentity.Instance,
                new[]
                {
                    "AccountAggregates", "AdvertisementAggregates", "ConsistencyAggregates", "FirmAggregates",
                    "PriceAggregates", "ProjectAggregates", "ThemeAggregates", "SystemAggregates"
                });

        public static SchemaInitializationCommand Messages { get; }
            = new SchemaInitializationCommand(Schema.Messages, DataObjectTypesProviderFactory.MessagesTypes.Concat(DataObjectTypesProviderFactory.ErmMessagesTypes).ToList(),
                ValidationRulesConnectionStringIdentity.Instance,
                new[] {"Messages", "MessagesCache"});

        public static SchemaInitializationCommand WebApp { get; }
            = new SchemaInitializationCommand(
                WebAppMappingSchemaHelper.GetWebAppMappingSchema(new VersionHelper().Version),
                WebAppMappingSchemaHelper.DataObjectTypes, ValidationRulesConnectionStringIdentity.Instance,
                new[] {"WebApp"});
    }
}