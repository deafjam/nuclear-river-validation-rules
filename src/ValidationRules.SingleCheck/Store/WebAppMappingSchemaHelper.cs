using LinqToDB.Mapping;
using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.ValidationRules.Replication;
using NuClear.ValidationRules.Storage;
using NuClear.ValidationRules.Storage.FieldComparer;
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

        private static readonly Type[] ExcludeAccessorEntityTypes =
        {
            // нет смысла считать этот агрегат Rulesets в single-check режиме
            // в нём нет данных из ERM, соответственно изменения в данных ERM на него не могут никак повлиять
            typeof(Storage.Model.Aggregates.PriceRules.Ruleset),
            typeof(Storage.Model.Aggregates.PriceRules.Ruleset.AdvertisementAmountRestriction),

            // нет смысла считать EntityName в single-check режиме
            // он всё равно не будет использоваться
            typeof(Storage.Model.Facts.EntityName),

            // понятие версии не определено для single-check режима
            typeof(Storage.Model.Messages.Version),
            typeof(Storage.Model.Messages.Version.ErmState),
        };

        // нет смысла создавать эти таблицы в схеме WebApp, т.к. выборка делается inmemory
        private static readonly Type[] ExcludeDataObjectTypes =
        {
            typeof(Storage.Model.Messages.Version.ValidationResult)
        };

        public static IEqualityComparerFactory EqualityComparerFactory { get; } = new EqualityComparerFactory(
            new LinqToDbPropertyProvider(Schemas),
            new XDocumentComparer(),
            new DateTimeComparer());

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
                .Except(ExcludeDataObjectTypes)
                .ToHashSet();
        }

        public static MappingSchema GetWebAppMappingSchema(string version)
        {
            var mappingSchema = new MappingSchema("WebApp", Schemas);
            var builder = mappingSchema.GetFluentMappingBuilder();

            foreach (var dataObjectType in DataObjectTypes)
            {
                var baseTable = mappingSchema.GetAttribute<TableAttribute>(dataObjectType);
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
            var markerAssembly = typeof(IValidationResultAccessor).Assembly;
            var accessorTypes = markerAssembly.GetTypes().Where(IsAccessorImplementation);

            var result = schemas.ToDictionary(x => x, x => new List<Type>());

            foreach (var accessorType in accessorTypes)
            {
                var interfaceType = accessorType.GetInterfaces().Single(IsAccessorInterface);
                var dataObjectType = GetAccessorDataObject(interfaceType);

                if (ExcludeAccessorEntityTypes.Contains(dataObjectType))
                {
                    continue;
                }

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