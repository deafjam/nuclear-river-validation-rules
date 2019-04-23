using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Storage
{
    public static partial class Schema
    {
        private const string MessagesSchema = "Messages";
        private const string CacheSchema = "MessagesCache";

        public static MappingSchema Messages { get; } =
            new MappingSchema(nameof(Messages), new SqlServerMappingSchema())
                .RegisterDataTypes()
                .GetFluentMappingBuilder()
                .RegisterMessages()
                .MappingSchema;

        private static FluentMappingBuilder RegisterMessages(this FluentMappingBuilder builder)
        {
            builder.Entity<Version>()
                   .HasSchemaName(MessagesSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<Version.ErmState>()
                   .HasSchemaName(MessagesSchema);

            builder.Entity<Version.ErmStateBulkDelete>()
                   .HasTableName(nameof(Version.ErmState))
                   .HasSchemaName(MessagesSchema);

            builder.Entity<Version.AmsState>()
                   .HasSchemaName(MessagesSchema);

            builder.Entity<Version.AmsStateBulkDelete>()
                   .HasTableName(nameof(Version.AmsState))
                   .HasSchemaName(MessagesSchema);

            builder.Entity<Version.ValidationResult>()
                   .HasSchemaName(MessagesSchema)
                   .HasIndex(x => new { x.Resolved })
                   .HasIndex(x => new { x.VersionId })
                   .HasIndex(x => new { x.Resolved, x.VersionId }, x => new { x.MessageType, x.MessageParams, x.PeriodStart, x.PeriodEnd, x.ProjectId, x.OrderId });

            builder.Entity<Version.ValidationResultBulkDelete>()
                   .HasTableName(nameof(Version.ValidationResult))
                   .HasSchemaName(MessagesSchema);

            builder.Entity<Cache.ValidationResult>()
                   .HasTableName(nameof(Cache.ValidationResult))
                   .HasSchemaName(CacheSchema)
                   .HasPrimaryKey(x => x.MessageType);

            return builder;
        }
    }
}