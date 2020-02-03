﻿using System;
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
    public sealed class RulesetQuantitativeRuleAccessor : IMemoryBasedDataObjectAccessor<Ruleset.QuantitativeRule>, IDataChangesHandler<Ruleset.QuantitativeRule>
    {
        // ReSharper disable once UnusedParameter.Local из-за использования hand-made DI в виде Activator.CreateInstance
        public RulesetQuantitativeRuleAccessor(IQuery _)
        {
        }

        public IReadOnlyCollection<Ruleset.QuantitativeRule> GetDataObjects(IEnumerable<ICommand> commands)
        {
            var dtos = commands
                .Cast<ReplaceDataObjectCommand>()
                .SelectMany(x => x.Dtos)
                .Cast<RulesetDto>()
                .GroupBy(x => x.Id)
                .Select(x => x.Aggregate((a,b) => a.Version > b.Version ? a : b));

            return dtos.SelectMany(ruleset => ruleset.QuantitativeRules
                                                     .Select(rule => new Ruleset.QuantitativeRule
                                                         {
                                                             RulesetId = ruleset.Id,
                                                             NomenclatureCategoryCode = rule.NomenclatureCategoryCode,
                                                             Min = rule.Min,
                                                             Max = rule.Max
                                                         }))
                       .ToList();
        }

        public FindSpecification<Ruleset.QuantitativeRule> GetFindSpecification(IEnumerable<ICommand> commands)
        {
            var ids = commands.Cast<ReplaceDataObjectCommand>().SelectMany(x => x.Dtos).Cast<RulesetDto>().Select(x => x.Id).ToHashSet();

            return new FindSpecification<Ruleset.QuantitativeRule>(x => ids.Contains(x.RulesetId));
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<Ruleset.QuantitativeRule> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<Ruleset.QuantitativeRule> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<Ruleset.QuantitativeRule> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<Ruleset.QuantitativeRule> dataObjects) => Array.Empty<IEvent>();
    }
}
