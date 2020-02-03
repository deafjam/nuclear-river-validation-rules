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
    public sealed class RulesetAssociatedRuleAccessor : IMemoryBasedDataObjectAccessor<Ruleset.AssociatedRule>, IDataChangesHandler<Ruleset.AssociatedRule>
    {
        public RulesetAssociatedRuleAccessor(IQuery _)
        {
        }

        public IReadOnlyCollection<Ruleset.AssociatedRule> GetDataObjects(IEnumerable<ICommand> commands)
        {
            var dtos = commands
                .Cast<ReplaceDataObjectCommand>()
                .SelectMany(x => x.Dtos)
                .Cast<RulesetDto>()
                .GroupBy(x => x.Id)
                .Select(x => x.Aggregate((a,b) => a.Version > b.Version ? a : b));
            
            return dtos.SelectMany(ruleset => ruleset.AssociatedRules
                                                     .Select(rule => new Ruleset.AssociatedRule
                                                         {
                                                             RulesetId = ruleset.Id,
                                                             PrincipalNomenclatureId = rule.NomeclatureId,
                                                             AssociatedNomenclatureId = rule.AssociatedNomenclatureId,
                                                             ConsideringBindingObject = rule.ConsideringBindingObject
                                                         }))
                       .ToList();
        }

        public FindSpecification<Ruleset.AssociatedRule> GetFindSpecification(IEnumerable<ICommand> commands)
        {
            var ids = commands.Cast<ReplaceDataObjectCommand>().SelectMany(x => x.Dtos).Cast<RulesetDto>().Select(x => x.Id).ToHashSet();

            return new FindSpecification<Ruleset.AssociatedRule>(x => ids.Contains(x.RulesetId));
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<Ruleset.AssociatedRule> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<Ruleset.AssociatedRule> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<Ruleset.AssociatedRule> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<Ruleset.AssociatedRule> dataObjects) => Array.Empty<IEvent>();
    }
}
