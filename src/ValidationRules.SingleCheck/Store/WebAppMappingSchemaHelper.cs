using LinqToDB.Mapping;
using NuClear.Replication.Core.DataObjects;
using NuClear.ValidationRules.Replication;
using NuClear.ValidationRules.Storage;
using NuClear.ValidationRules.Storage.Model.Facts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NuClear.ValidationRules.SingleCheck.Store
{
    public static class WebAppMappingSchemaHelper
    {
        public static IReadOnlyCollection<Type> FactsAccessorTypes { get; }
        public static IReadOnlyCollection<Type> AggregatesAccessorTypes { get; }
        public static IReadOnlyCollection<Type> MessagesAccessorTypes { get; }

        public static IReadOnlyCollection<Type> DataObjectTypes { get; }

        private static readonly MappingSchema[] Schemas = { Schema.Facts, Schema.Aggregates, Schema.Messages };

        static WebAppMappingSchemaHelper()
        {
            var accessorTypes = ScanForAccessors(Schemas);

            FactsAccessorTypes = accessorTypes[Schema.Facts];
            AggregatesAccessorTypes = accessorTypes[Schema.Aggregates];
            MessagesAccessorTypes = accessorTypes[Schema.Messages];

            DataObjectTypes = accessorTypes
                .SelectMany(x => x.Value)
                .Select(x => x.GetInterfaces().Single(IsAccessorInterface))
                .Select(GetAccessorDataObject)
                .Concat(new[]
                {
                    // TODO: разобраться почему так
                    typeof(Ruleset),
                    typeof(Ruleset.AssociatedRule),
                    typeof(Ruleset.DeniedRule),
                    typeof(Ruleset.QuantitativeRule),
                    typeof(Ruleset.RulesetProject)
                })
                .ToHashSet();
        }

        public static MappingSchema GetWebAppMappingSchema(string version)
        {
            var baseSchema = new MappingSchema(Schemas);
            var mappingSchema = new MappingSchema("WebApp", baseSchema);
            var builder = mappingSchema.GetFluentMappingBuilder();

            foreach (var dataObjectType in DataObjectTypes)
            {
                var baseTable = baseSchema.GetAttribute<TableAttribute>(dataObjectType);
                if (baseTable != null)
                {
                    var attribute = new TableAttribute { Name = $"{baseTable.Schema}_{baseTable.Name ?? dataObjectType.Name}_{version}", Schema = "WebApp", IsColumnAttributeRequired = false };
                    builder.HasAttribute(dataObjectType, attribute);
                }
            }

            return mappingSchema;
        }

        private static IReadOnlyDictionary<MappingSchema, List<Type>> ScanForAccessors(IReadOnlyCollection<MappingSchema> schemas)
        {
            var accessorTypes = typeof(IValidationResultAccessor).Assembly.GetTypes().Where(IsAccessorImplementation);

            var result = schemas.ToDictionary(x => x, x => new List<Type>());

            foreach (var accessorType in accessorTypes)
            {
                var interfaceType = accessorType.GetInterfaces().Single(IsAccessorInterface);
                var dataObjectType = GetAccessorDataObject(interfaceType);

                foreach (var schema in schemas)
                {
                    if (schema.GetAttribute<TableAttribute>(dataObjectType) != null)
                    {
                        result[schema].Add(accessorType);
                        break;
                    }
                }
            }

            return result;
        }

        private static bool IsAccessorImplementation(Type type)
            => type.IsClass && !type.IsAbstract && type.GetInterfaces().Any(IsAccessorInterface);

        private static bool IsAccessorInterface(Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IStorageBasedDataObjectAccessor<>);

        private static Type GetAccessorDataObject(Type type)
            => type.GetGenericArguments().Single();
    }
}