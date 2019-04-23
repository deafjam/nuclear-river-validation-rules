using NuClear.ValidationRules.SingleCheck.Store;

namespace NuClear.ValidationRules.SingleCheck
{
    public sealed class PipelineFactory
    {
        public Pipeline Create(string version)
        {
            return new Pipeline(
                WebAppMappingSchemaHelper.FactsAccessorTypes,
                WebAppMappingSchemaHelper.AggregatesAccessorTypes,
                WebAppMappingSchemaHelper.MessagesAccessorTypes,
                WebAppMappingSchemaHelper.GetWebAppMappingSchema(version));
        }
    }
}