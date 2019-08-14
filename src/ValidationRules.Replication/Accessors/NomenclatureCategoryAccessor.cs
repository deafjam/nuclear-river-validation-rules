using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Specs;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Events;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.Accessors
{
    public sealed class NomenclatureCategoryAccessor : IStorageBasedDataObjectAccessor<NomenclatureCategory>, IDataChangesHandler<NomenclatureCategory>
    {
        private readonly IQuery _query;

        public NomenclatureCategoryAccessor(IQuery query) => _query = query;

        public IQueryable<NomenclatureCategory> GetSource()
            => _query.For(Specs.Find.Erm.NomenclatureCategory)
                .Select(x => new NomenclatureCategory {Id = x.Id});

        public FindSpecification<NomenclatureCategory> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<NomenclatureCategory>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<NomenclatureCategory> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<NomenclatureCategory> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<NomenclatureCategory> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<NomenclatureCategory> dataObjects)
        {
            var ids = dataObjects.Select(x => x.Id).ToHashSet();

            var rulesetIds = _query.For<Ruleset.QuantitativeRule>()
                                   .Where(r => ids.Contains(r.NomenclatureCategoryCode))
                                   .Select(r => r.RulesetId)
                                   .Distinct()
                                   .ToList();

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(NomenclatureCategory), typeof(Ruleset), rulesetIds)};
        }
    }
}