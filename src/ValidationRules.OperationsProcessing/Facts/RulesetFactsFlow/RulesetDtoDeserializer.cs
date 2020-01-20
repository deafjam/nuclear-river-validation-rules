using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Confluent.Kafka;
using NuClear.ValidationRules.Hosting.Common.Settings;
using NuClear.ValidationRules.Replication.Dto;

using Optional;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.RulesetFactsFlow
{
    public sealed class RulesetDtoDeserializer : IDeserializer<ConsumeResult<Ignore, byte[]>, RulesetDto>
    {
        private readonly string _targetBusinessModelAlias;
        private static readonly Regex ExtractBusinessModelSuffixRegex = new Regex(@"(?:.+\.)+(?<suffix>\w+)", RegexOptions.Compiled);

        public RulesetDtoDeserializer(IBusinessModelSettings businessModelSettings)
        {
            _targetBusinessModelAlias = Convert2SourceCode(businessModelSettings.BusinessModel);
        }

        public IEnumerable<RulesetDto> Deserialize(IEnumerable<ConsumeResult<Ignore, byte[]>> consumeResults) =>
            consumeResults
                // filter heartbeat & tombstone messages
                .Where(x => x.Value != null)
                .Select(x =>
                {
                    var rawXmlRulesetMessage = Encoding.UTF8.GetString(x.Value);
                    var xmlRulesetMessage = XElement.Parse(rawXmlRulesetMessage);

                    var sourceBusinessModel = xmlRulesetMessage.Attribute("SourceCode")
                        .SomeNotNull()
                        .Map(a => a.Value)
                        .FlatMap(ExtractBusinessModelSuffix)
                        .ValueOr(() => throw new InvalidOperationException("Required attribute \"SourceCode\" was not found"));
                    if (!string.Equals(sourceBusinessModel, _targetBusinessModelAlias, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // сообщение предназначено для другой businessmodel
                        return (RulesetDto) null;
                    }

                    return ConvertToRulesetDto(xmlRulesetMessage);                    
                })
                .Where(x => x != null);

        private Option<string> ExtractBusinessModelSuffix(string rawValue)
        {
            if (rawValue == null)
            {
                throw new ArgumentNullException(nameof(rawValue));
            }

            return ExtractBusinessModelSuffixRegex.Match(rawValue)
                                                  .SomeWhen(match => match.Success)
                                                  .Map(match => match.Groups["suffix"].Value);
        }

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

        private static string Convert2SourceCode(string businessModel) =>
            businessModel switch
            {
                "Russia" => "RU",
                "Cyprus" => "CY",
                "Czech" => "CZ",
                "Ukraine" => "UA",
                "Emirates" => "AE",
                "Kazakhstan" => "KZ",
                "Kyrgyzstan" => "KG",
                "Uzbekistan" => "UZ",
                "Azerbaijan" => "AZ",
                _ => throw new ArgumentOutOfRangeException()
            };
    }
}