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
    public sealed class FirmInactiveAccessor : IStorageBasedDataObjectAccessor<FirmInactive>, IDataChangesHandler<FirmInactive>
    {
        private readonly IQuery _query;

        public FirmInactiveAccessor(IQuery query) => _query = query;

        public IQueryable<FirmInactive> GetSource() => _query
            .For(Specs.Find.Erm.Firm.Inactive)
            .Select(x => new FirmInactive
            {
                Id = x.Id,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                IsClosedForAscertainment = x.ClosedForAscertainment
            });

        public FindSpecification<FirmInactive> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<FirmInactive>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<FirmInactive> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<FirmInactive> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<FirmInactive> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<FirmInactive> dataObjects)
        {
            var firmIds = dataObjects.Select(x => x.Id).ToHashSet();

            var orderIds =
                from order in _query.For<Order>().Where(x => firmIds.Contains(x.FirmId))
                select order.Id;

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(FirmInactive), typeof(Order), orderIds.ToHashSet())};
        }
    }
}