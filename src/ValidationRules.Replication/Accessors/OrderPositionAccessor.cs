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

namespace NuClear.ValidationRules.Replication.Accessors
{
    public sealed class OrderPositionAccessor : IStorageBasedDataObjectAccessor<OrderPosition>, IDataChangesHandler<OrderPosition>
    {
        private readonly IQuery _query;

        public OrderPositionAccessor(IQuery query) => _query = query;

        public IQueryable<OrderPosition> GetSource() =>
                // join тут можно использовать, т.к. OrderPosition\OrderPositionAdvertisement это ValueObjects для Order
                from order in _query.For(Specs.Find.Erm.Order)
                from orderPosition in _query.For(Specs.Find.Erm.OrderPosition).Where(x => x.OrderId == order.Id)
                select new OrderPosition
                   {
                       Id = orderPosition.Id,
                       OrderId = orderPosition.OrderId,
                       PricePositionId = orderPosition.PricePositionId,
                   };

        public FindSpecification<OrderPosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<OrderPosition>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<OrderPosition> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<OrderPosition> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<OrderPosition> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<OrderPosition> dataObjects)
        {
            var orderIds = dataObjects.Select(x => x.OrderId).ToHashSet();

            var accountIds =
                from order in _query.For<Order>().Where(x => orderIds.Contains(x.Id))
                from account in _query.For<Account>().Where(x => x.LegalPersonId == order.LegalPersonId && x.BranchOfficeOrganizationUnitId == order.BranchOfficeOrganizationUnitId)
                select account.Id;

            return new[]
            {
                new RelatedDataObjectOutdatedEvent(typeof(OrderPosition), typeof(Order), orderIds),
                new RelatedDataObjectOutdatedEvent(typeof(OrderPosition), typeof(Account), accountIds.ToHashSet())
            };
        }
    }
}