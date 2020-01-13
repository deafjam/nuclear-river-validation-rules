using NuClear.DataTest.Metamodel;
using NuClear.Metamodeling.Elements;
using NuClear.Metamodeling.Provider.Sources;
using NuClear.ValidationRules.Hosting.Common.Identities.Connections;
using NuClear.ValidationRules.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public class SchemaMetadataSource : MetadataSourceBase<SchemaMetadataIdentity>
    {
        private static readonly SchemaMetadataElement Erm = SchemaMetadataElement.Config
            .For(ContextName.Erm)
            .HasConnectionString<ErmConnectionStringIdentity>()
            .HasSchema(Schema.Erm);

        private static readonly SchemaMetadataElement Facts = SchemaMetadataElement.Config
            .For(ContextName.Facts)
            .HasConnectionString<ValidationRulesConnectionStringIdentity>()
            .HasSchema(Schema.Facts);

        private static readonly SchemaMetadataElement Aggregates = SchemaMetadataElement.Config
            .For(ContextName.Aggregates)
            .HasConnectionString<ValidationRulesConnectionStringIdentity>()
            .HasSchema(Schema.Aggregates);

        private static readonly SchemaMetadataElement Messages = SchemaMetadataElement.Config
            .For(ContextName.Messages)
            .HasConnectionString<ValidationRulesConnectionStringIdentity>()
            .HasSchema(Schema.Messages);

        public SchemaMetadataSource(IEnumerable<string> requiredContexts)
        {
            Metadata = new[] { Erm, Facts, Aggregates, Messages }
                       .Where(x => requiredContexts.Contains(x.Context))
                       .OfType<IMetadataElement>()
                       .ToDictionary(x => x.Identity.Id);
        }

        public override IReadOnlyDictionary<Uri, IMetadataElement> Metadata { get; }
    }
}
