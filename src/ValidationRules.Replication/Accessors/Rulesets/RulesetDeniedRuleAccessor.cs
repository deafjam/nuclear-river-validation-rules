using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Dto;
using NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.Accessors.Rulesets
{
    public sealed class RulesetDeniedRuleAccessor : IMemoryBasedDataObjectAccessor<Ruleset.DeniedRule>, IDataChangesHandler<Ruleset.DeniedRule>
    {
        public RulesetDeniedRuleAccessor(IQuery _)
        {
        }

        public IReadOnlyCollection<Ruleset.DeniedRule> GetDataObjects(IEnumerable<ICommand> commands)
        {
            var dtos = commands
                .Cast<ReplaceDataObjectCommand>()
                .SelectMany(x => x.Dtos)
                .Cast<RulesetDto>()
                .GroupBy(x => x.Id)
                .Select(x => x.Aggregate((a,b) => a.Version > b.Version ? a : b));

            var targetRules = dtos.SelectMany(ruleset => ruleset.DeniedRules
                                                                .Select(rule => new Ruleset.DeniedRule
                                                                    {
                                                                        RulesetId = ruleset.Id,
                                                                        NomenclatureId = rule.NomeclatureId,
                                                                        DeniedNomenclatureId = rule.DeniedNomenclatureId,
                                                                        BindingObjectStrategy = rule.BindingObjectStrategy
                                                                    }))
                                  .ToList();
            var symmetricRules = targetRules.Where(rule => rule.NomenclatureId != rule.DeniedNomenclatureId)
                                            .Select(rule => new Ruleset.DeniedRule
                                                {
                                                    RulesetId = rule.RulesetId,
                                                    NomenclatureId = rule.DeniedNomenclatureId,
                                                    DeniedNomenclatureId = rule.NomenclatureId,
                                                    BindingObjectStrategy = rule.BindingObjectStrategy
                                                });

            return targetRules.Concat(symmetricRules)
                              .ToList();
        }

        public FindSpecification<Ruleset.DeniedRule> GetFindSpecification(IEnumerable<ICommand> commands)
        {
            var ids = commands.Cast<ReplaceDataObjectCommand>().SelectMany(x => x.Dtos).Cast<RulesetDto>().Select(x => x.Id).ToHashSet();

            return new FindSpecification<Ruleset.DeniedRule>(x => ids.Contains(x.RulesetId));
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<Ruleset.DeniedRule> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<Ruleset.DeniedRule> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<Ruleset.DeniedRule> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<Ruleset.DeniedRule> dataObjects) => Array.Empty<IEvent>();
    }
}
