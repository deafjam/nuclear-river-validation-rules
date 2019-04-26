using LinqToDB.Data;
using NuClear.ValidationRules.Storage;

namespace NuClear.ValidationRules.Querying.Host.DataAccess
{
    public class DataConnectionFactory
    {
        public DataConnection CreateDataConnection()
        {
            var connection = new DataConnection("ValidationRules");
            connection
                // Schema.Facts needed for Facts.EntityName table
                .AddMappingSchema(Schema.Facts)
                .AddMappingSchema(Schema.Messages);
            return connection;
        }
    }
}