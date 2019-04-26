using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using NuClear.Storage.API.ConnectionStrings;
using NuClear.ValidationRules.Storage.SchemaInitializer;
using System;
using System.Linq;
using System.Transactions;

namespace NuClear.ValidationRules.StateInitialization.Host
{
    internal sealed class WebAppSchemaInitializationHelper
    {
        private readonly IConnectionStringSettings _connectionStringSettings;

        public WebAppSchemaInitializationHelper(IConnectionStringSettings connectionStringSettings)
        {
            _connectionStringSettings = connectionStringSettings;
        }

        public void CreateWebAppSchema(SchemaInitializationCommand command)
        {
            using (var dataConnection = CreateDataConnection(command.ConnectionStringIdentity, command.MappingSchema))
            {
                var service = new SqlSchemaService(dataConnection);
                var excludeTableNames = service.AllTables().Select(x => x.TableName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var tablesToCreate = command.DataTypes.Where(x => !excludeTableNames.Contains(command.MappingSchema.GetAttribute<TableAttribute>(x).Name));

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    service.CreateTables(tablesToCreate);
                    scope.Complete();
                }
            }
        }

        public void DropWebAppSchema(SchemaInitializationCommand command)
        {
            var excludeTableNames = command.DataTypes
                .Select(x => command.MappingSchema.GetAttribute<TableAttribute>(x))
                .Select(x => x.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            using (var dataConnection = CreateDataConnection(command.ConnectionStringIdentity, command.MappingSchema))
            {
                var service = new SqlSchemaService(dataConnection);
                var allTables = service.AllTables();

                var tablesToDelete = allTables
                    .Where(x => command.SqlSchemas.Contains(x.SchemaName))
                    .Where(x => !excludeTableNames.Contains(x.TableName));

                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                {
                    service.DropTables(tablesToDelete);
                    scope.Complete();
                }
            }
        }

        private DataConnection CreateDataConnection(IConnectionStringIdentity connectionStringIdentity, MappingSchema mappingSchema)
        {
            var connectionString = _connectionStringSettings.GetConnectionString(connectionStringIdentity);
            var dataConnection = SqlServerTools.CreateDataConnection(connectionString);
            dataConnection.AddMappingSchema(mappingSchema);
            dataConnection.CommandTimeout = 0;
            return dataConnection;
        }
    }
}