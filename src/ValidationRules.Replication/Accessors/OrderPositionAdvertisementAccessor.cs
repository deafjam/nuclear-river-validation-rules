using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Specs;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Events;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Model.Facts;
using System;
using System.Collections.Generic;
using System.Linq;
using Erm = NuClear.ValidationRules.Storage.Model.Erm;

namespace NuClear.ValidationRules.Replication.Accessors
{
    public sealed class OrderPositionAdvertisementAccessor : IStorageBasedDataObjectAccessor<OrderPositionAdvertisement>, IDataChangesHandler<OrderPositionAdvertisement>
    {
        private readonly IQuery _query;

        public OrderPositionAdvertisementAccessor(IQuery query) => _query = query;

        public IQueryable<OrderPositionAdvertisement> GetSource()
            =>
                // join тут можно использовать, т.к. OrderPosition\OrderPositionAdvertisement это ValueObjects для Order 
                from order in _query.For(Specs.Find.Erm.Order)
                from op in _query.For(Specs.Find.Erm.OrderPosition).Where(x => x.OrderId == order.Id)
                from opa in _query.For<Erm::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == op.Id)
                select new OrderPositionAdvertisement
                {
                    OrderPositionId = opa.OrderPositionId,
                    OrderId = op.OrderId,
                    PositionId = opa.PositionId,

                    FirmAddressId = opa.FirmAddressId,
                    CategoryId = opa.CategoryId,
                    AdvertisementId = opa.AdvertisementId,
                    ThemeId = opa.ThemeId,
                };

        public FindSpecification<OrderPositionAdvertisement> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<OrderPositionAdvertisement>.Contains(x => x.OrderPositionId, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<OrderPositionAdvertisement> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<OrderPositionAdvertisement> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<OrderPositionAdvertisement> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<OrderPositionAdvertisement> dataObjects)
        {
            var orderIds = dataObjects.Select(x => x.OrderId).ToHashSet();

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(OrderPositionAdvertisement), typeof(Order), orderIds)};
        }
    }
}