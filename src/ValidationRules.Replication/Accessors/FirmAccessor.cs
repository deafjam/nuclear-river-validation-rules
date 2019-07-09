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
    public sealed class FirmAccessor : IStorageBasedDataObjectAccessor<Firm>, IDataChangesHandler<Firm>
    {
        private readonly IQuery _query;

        public FirmAccessor(IQuery query) => _query = query;

        public IQueryable<Firm> GetSource() => _query
            .For(Specs.Find.Erm.Firm.Active)
            .Select(x => new Firm
            {
                Id = x.Id,
                OrganizationUnitId = x.OrganizationUnitId,
            });

        public FindSpecification<Firm> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<Firm>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<Firm> dataObjects)
            => new [] {new DataObjectCreatedEvent(typeof(Firm), dataObjects.Select(x => x.Id)) };

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<Firm> dataObjects)
            => new [] {new DataObjectUpdatedEvent(typeof(Firm), dataObjects.Select(x => x.Id)) };

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<Firm> dataObjects)
            => new [] {new DataObjectDeletedEvent(typeof(Firm), dataObjects.Select(x => x.Id)) };

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<Firm> dataObjects)
        {
            var firmIds = dataObjects.Select(x => x.Id).ToHashSet();

            var orderIds =
                from order in _query.For<Order>().Where(x => firmIds.Contains(x.FirmId))
                select order.Id;

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(Firm), typeof(Order), orderIds.ToHashSet())};
        }
    }
}