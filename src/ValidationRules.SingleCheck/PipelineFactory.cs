using NuClear.ValidationRules.SingleCheck.Store;
using NuClear.ValidationRules.SingleCheck.Tenancy;

namespace NuClear.ValidationRules.SingleCheck
{
    public sealed class PipelineFactory
    {
        private readonly IDataConnectionProvider _connectionProvider;

        public PipelineFactory(IDataConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public Pipeline Create(string version)
        {
            return new Pipeline(
                WebAppMappingSchemaHelper.FactsAccessorTypes,
                WebAppMappingSchemaHelper.AggregatesAccessorTypes,
                WebAppMappingSchemaHelper.MessagesAccessorTypes,
                WebAppMappingSchemaHelper.EqualityComparerFactory,
                WebAppMappingSchemaHelper.GetWebAppMappingSchema(version),
                _connectionProvider);
        }
    }
}
