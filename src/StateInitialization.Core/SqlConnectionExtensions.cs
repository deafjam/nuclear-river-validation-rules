using System.Data;
using System.Data.SqlClient;

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core
{
    public static class SqlConnectionExtensions
    {
        public static Database GetDatabase(this SqlConnection sqlConnection)
        {
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }

            var connection = new ServerConnection(sqlConnection) { StatementTimeout = 0 };
            var server = new Server(connection);

            var connectionStringBuilder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
            return server.Databases[connectionStringBuilder.InitialCatalog];
        }

        public static Table GetTable(this Database database, TableName tableName)
        {
            if (string.IsNullOrEmpty(tableName.Schema))
            {
                if (database.Tables.Contains(tableName.Table))
                {
                    return database.Tables[tableName.Table];
                }
            }
            else
            {
                if (database.Tables.Contains(tableName.Table, tableName.Schema))
                {
                    return database.Tables[tableName.Table, tableName.Schema];
                }
            }

            return null;
        }
    }
}
