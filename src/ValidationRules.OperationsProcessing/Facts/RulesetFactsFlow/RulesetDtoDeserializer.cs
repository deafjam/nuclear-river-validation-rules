using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Confluent.Kafka;
using NuClear.ValidationRules.Replication.Dto;

using Optional;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.RulesetFactsFlow
{
    public sealed class RulesetDtoDeserializer : IDeserializer<ConsumeResult<Ignore, byte[]>, RulesetDto>
    {
        public IEnumerable<RulesetDto> Deserialize(IEnumerable<ConsumeResult<Ignore, byte[]>> consumeResults) =>
            consumeResults
                // filter heartbeat & tombstone messages
                .Where(x => x.Value != null)
                .Select(x =>
                {
                    var rawXmlRulesetMessage = Encoding.UTF8.GetString(x.Value);
                    var xmlRulesetMessage = XElement.Parse(rawXmlRulesetMessage);
                    return ConvertToRulesetDto(xmlRulesetMessage);
                })
                .Where(x => x != null);

        private static RulesetDto ConvertToRulesetDto(XElement rulesetXml)
        {
            var rulesElements = rulesetXml.Element("Rules");
            return new RulesetDto
                {
                    Id = (long)rulesetXml.Attribute("Code"),
                    BeginDate = (DateTime)rulesetXml.Attribute("BeginDate"),
                    EndDate = (DateTime?)rulesetXml.Attribute("EndDate"),
                    IsDeleted = (bool?)rulesetXml.Attribute("IsDeleted") ?? false,
                    Version = (int)rulesetXml.Attribute("Version"),
                    AssociatedRules = rulesElements.Element("Associated")
                                                   .Elements("Rule")
                                                   .Select(Convert2AssociatedRule)
                                                   .ToList(),
                    DeniedRules = rulesElements.Element("Denied")
                                               .Elements("Rule")
                                               .Select(Convert2DeniedRule)
                                               .ToList(),
                    QuantitativeRules = rulesElements.Element("Quantitative")
                                                     .Elements("Rule")
                                                     .Select(Convert2QuantitativeRule)
                                                     .ToList(),
                    Projects = rulesetXml.Element("Branches")
                                         .Elements("Branch")
                                         .Select(b => (long)b.Attribute("Code"))
                                         .ToList()
                };
        }

        private static RulesetDto.AssociatedRule Convert2AssociatedRule(XElement ruleElement) =>
            new RulesetDto.AssociatedRule
            {
                NomeclatureId = (long)ruleElement.Attribute("PrincipalNomenclatureCode"),
                AssociatedNomenclatureId = (long)ruleElement.Attribute("AssociatedNomenclatureCode"),
                ConsideringBindingObject = (bool)ruleElement.Attribute("IsConsiderBindingObject")
            };

        private static RulesetDto.DeniedRule Convert2DeniedRule(XElement ruleElement)
        {
            var nomenclaturesElements = ruleElement.Element("Nomenclatures")
                                                   .Elements("Nomenclature")
                                                   .Select(n => (long)n.Attribute("Code"))
                                                   .ToList();

            if (nomenclaturesElements.Count != 2)
            {
                throw new InvalidOperationException("Denied rule element have to contain exactly 2 nomenclature sub elements");
            }

            return new RulesetDto.DeniedRule
                {
                    NomeclatureId = nomenclaturesElements[0],
                    DeniedNomenclatureId = nomenclaturesElements[1],
                    BindingObjectStrategy = ConvertBindingObjectStrategy(ruleElement.Attribute("BindingTypeStrategy")?.Value)
                };
        }

        private static RulesetDto.QuantitativeRule Convert2QuantitativeRule(XElement ruleElement) =>
            new RulesetDto.QuantitativeRule
            {
                NomenclatureCategoryCode = (long)ruleElement.Attribute("NomenclatureCategoryCode"),
                Min = (int)ruleElement.Attribute("Min"),
                Max = (int)ruleElement.Attribute("Max"),
            };

        private static int ConvertBindingObjectStrategy(string rawValue) =>
            rawValue switch
            {
                "Match" => 1,
                "NoDependency" => 2,
                "Different" => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(rawValue), rawValue)
            };
    }
}
