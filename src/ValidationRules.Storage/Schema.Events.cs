using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using NuClear.ValidationRules.Storage.Model.Events;

namespace NuClear.ValidationRules.Storage
{
    public static partial class Schema
    {
        private const string EventsSchema = "Events";

        public static MappingSchema Events { get; } =
            new MappingSchema(nameof(Events), new SqlServerMappingSchema())
                .RegisterDataTypes()
                .GetFluentMappingBuilder()
                .RegisterEvents()
                .MappingSchema;

        private static FluentMappingBuilder RegisterEvents(this FluentMappingBuilder builder)
        {
            builder.Entity<EventRecord>()
                   .HasSchemaName(EventsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIdentity(x => x.Id);

            return builder;
        }
    }
}