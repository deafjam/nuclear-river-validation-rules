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
    public sealed class LegalPersonProfileAccessor : IStorageBasedDataObjectAccessor<LegalPersonProfile>, IDataChangesHandler<LegalPersonProfile>
    {
        private readonly IQuery _query;

        public LegalPersonProfileAccessor(IQuery query) => _query = query;

        public IQueryable<LegalPersonProfile> GetSource() => _query
            .For(Specs.Find.Erm.LegalPersonProfile)
            .Select(x => new LegalPersonProfile
                {
                    Id = x.Id,
                    LegalPersonId = x.LegalPersonId,
                    BargainEndDate = x.BargainEndDate,
                    WarrantyEndDate = x.WarrantyEndDate,
                });

        public FindSpecification<LegalPersonProfile> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<LegalPersonProfile>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<LegalPersonProfile> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<LegalPersonProfile> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<LegalPersonProfile> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<LegalPersonProfile> dataObjects)
        {
            var legalPersonProfileIds = dataObjects.Select(x => x.Id).ToHashSet();
            var legalPersonIds = dataObjects.Select(x => x.LegalPersonId).ToHashSet();

            var orderIds =
                (from order in _query.For<OrderConsistency>()
                where legalPersonProfileIds.Contains(order.LegalPersonProfileId.Value)
                      || legalPersonIds.Contains(order.LegalPersonId.Value)
                select order.Id)
                .Distinct()
                .ToList();

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(LegalPersonProfile), typeof(Order), orderIds)};
        }
    }
}