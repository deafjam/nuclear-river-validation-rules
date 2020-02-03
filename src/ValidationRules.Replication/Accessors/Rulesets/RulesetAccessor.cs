using System;
using System.Collections.Generic;
using System.Linq;
using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Dto;
using NuClear.ValidationRules.Replication.Events;
using NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.Accessors.Rulesets
{
    public sealed class RulesetAccessor : IMemoryBasedDataObjectAccessor<Ruleset>, IDataChangesHandler<Ruleset>
    {
        private readonly IQuery _query;

        public RulesetAccessor(IQuery query) => _query = query;

        public IReadOnlyCollection<Ruleset> GetDataObjects(IEnumerable<ICommand> commands)
        {
            var dtos = commands
                .Cast<ReplaceDataObjectCommand>()
                .SelectMany(x => x.Dtos)
                .Cast<RulesetDto>()
                .GroupBy(x => x.Id)
                .Select(x => x.Aggregate((a,b) => a.Version > b.Version ? a : b));
            
            var now = DateTime.UtcNow;
            return dtos.Select(x => new Ruleset
                {
                    Id = x.Id,
                    BeginDate = x.BeginDate,
                    EndDate = x.EndDate?.Add(TimeSpan.FromSeconds(1)) ?? DateTime.MaxValue,
                    IsDeleted = x.IsDeleted,
                    Version = x.Version,
                    ImportedOn = now
                })
                .ToList();
        }

        public FindSpecification<Ruleset> GetFindSpecification(IEnumerable<ICommand> commands)
        {
            var ids = commands.Cast<ReplaceDataObjectCommand>().SelectMany(x => x.Dtos).Cast<RulesetDto>().Select(x => x.Id).ToHashSet();

            return new FindSpecification<Ruleset>(x => ids.Contains(x.Id));
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<Ruleset> dataObjects)
            => new[] {new DataObjectCreatedEvent(typeof(Ruleset), dataObjects.Select(x => x.Id))};

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<Ruleset> dataObjects) =>
            Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<Ruleset> dataObjects) =>
            Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<Ruleset> dataObjects)
        {
            var rulesetsIds = dataObjects.Select(x => x.Id).ToHashSet();

            var firmIds = (from ruleset in _query.For<Ruleset>().Where(x => rulesetsIds.Contains(x.Id))
                from rulesetProject in _query.For<Ruleset.RulesetProject>().Where(x => x.RulesetId == ruleset.Id)
                from order in _query.For<Order>()
                    .Where(x => ruleset.BeginDate <= x.AgileDistributionStartDate
                                && x.AgileDistributionStartDate < ruleset.EndDate
                                && x.ProjectId == rulesetProject.ProjectId)
                select order.FirmId)
                .Distinct()
                .ToList();

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(Ruleset), typeof(Firm), firmIds)};
        }
    }
}