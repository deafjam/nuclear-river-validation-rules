using System;
using System.Linq;

using LinqToDB.Mapping;

using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core
{
    public static class MappingSchemaExtensions
    {
        public static TableName GetTableName(this MappingSchema mappingSchema, Type dataObjectType)
        {
            var attribute = mappingSchema
                .GetAttributes<TableAttribute>(dataObjectType)
                .FirstOrDefault();

            var tableName = attribute?.Name ?? dataObjectType.Name;
            var schemaName = attribute?.Schema;
            return new TableName(tableName, schemaName);
        }
    }
}
