using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Configuration;
using System.Linq;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using NuClear.ValidationRules.SingleCheck.Tenancy;
using NuClear.ValidationRules.Storage;

namespace NuClear.ValidationRules.Querying.Host.Tenancy
{
    public class DataConnectionProvider : IDataConnectionProvider
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly IReadOnlyDictionary<string, string> _connectionStrings;

        public DataConnectionProvider(ITenantProvider tenantProvider)
        {
            _tenantProvider = tenantProvider;
            _connectionStrings = WebConfigurationManager.ConnectionStrings
                .Cast<ConnectionStringSettings>()
                .ToDictionary(x => x.Name, x => x.ConnectionString);
        }

        public DataConnection CreateConnection(string connectionStringName)
        {
            var dataConnection =  new DataConnection(
                SqlServerTools.GetDataProvider(SqlServerVersion.v2012),
                GetConnectionString(connectionStringName));
            Configure(dataConnection, connectionStringName);
            return dataConnection;
        }

        private void Configure(DataConnection dataConnection, string connectionStringName)
        {
            if (string.Equals(DataConnectionName.ValidationRules, connectionStringName,
                StringComparison.InvariantCultureIgnoreCase))
            {
                // Schema.Facts needed for Facts.EntityName table
                dataConnection.AddMappingSchema(Schema.Facts);
                dataConnection.AddMappingSchema(Schema.Messages);
            }

            if (string.Equals(DataConnectionName.Erm, connectionStringName,
                StringComparison.InvariantCultureIgnoreCase))
            {

            }
        }

        private string GetConnectionString(string connectionStringName)
        {
            var connectionString = default(string);
            var tenantConnectionStringName = $"{connectionStringName}.{_tenantProvider.Current:G}";
            if (_connectionStrings.TryGetValue(tenantConnectionStringName, out connectionString)
                && !string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            if (_connectionStrings.TryGetValue(connectionStringName, out connectionString)
                && !string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            throw new ArgumentException(
                $"No connection string configured for '{connectionStringName}'",
                nameof(connectionStringName));
        }
    }
}
