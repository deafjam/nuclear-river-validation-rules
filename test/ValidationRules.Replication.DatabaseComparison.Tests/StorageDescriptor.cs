using LinqToDB.Mapping;
using NuClear.ValidationRules.Storage;

namespace ValidationRules.Replication.DatabaseComparison.Tests
{
    public sealed class StorageDescriptor
    {
        public static StorageDescriptor Erm = new StorageDescriptor(Schema.Erm, "Erm");
        public static StorageDescriptor Facts = new StorageDescriptor(Schema.Facts, "ValidationRules");
        public static StorageDescriptor Aggregates = new StorageDescriptor(Schema.Aggregates, "ValidationRules");
        public static StorageDescriptor Messages = new StorageDescriptor(Schema.Messages, "ValidationRules");

        public StorageDescriptor(MappingSchema mappingSchema, string connectionStringName)
        {
            MappingSchema = mappingSchema;
            ConnectionStringName = connectionStringName;
        }

        public MappingSchema MappingSchema { get; }
        public string ConnectionStringName { get; }
    }
}