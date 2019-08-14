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

using Erm = NuClear.ValidationRules.Storage.Model.Erm;

namespace NuClear.ValidationRules.Replication.Accessors
{
    public sealed class OrderItemAccessor : IStorageBasedDataObjectAccessor<OrderItem>, IDataChangesHandler<OrderItem>
    {
        private readonly IQuery _query;

        public OrderItemAccessor(IQuery query) => _query = query;

        public IQueryable<OrderItem> GetSource()
        {
            // join тут можно использовать, т.к. OrderPosition\OrderPositionAdvertisement это ValueObjects для Order
            var opas =
                from order in _query.For(Specs.Find.Erm.Order)
                from orderPosition in _query.For(Specs.Find.Erm.OrderPosition).Where(x => x.OrderId == order.Id)
                from pricePosition in _query.For<Erm::PricePosition>().Where(x => x.Id == orderPosition.PricePositionId)
                from opa in _query.For<Erm::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == orderPosition.Id)
                select new OrderItem
                    {
                        OrderId = orderPosition.OrderId,
                        OrderPositionId = orderPosition.Id,
                        PackagePositionId = pricePosition.PositionId,
                        ItemPositionId = opa.PositionId,

                        FirmAddressId = opa.FirmAddressId,
                        CategoryId = opa.CategoryId,
                    };

            var pkgs =
                from order in _query.For(Specs.Find.Erm.Order)
                from orderPosition in _query.For(Specs.Find.Erm.OrderPosition).Where(x => x.OrderId == order.Id)
                from pricePosition in _query.For<Erm::PricePosition>().Where(x => x.Id == orderPosition.PricePositionId)
                from _ in _query.For<Erm::PositionChild>().Where(x => x.MasterPositionId == pricePosition.PositionId)
                select new OrderItem
                    {
                        OrderId = orderPosition.OrderId,
                        OrderPositionId = orderPosition.Id,
                        PackagePositionId = pricePosition.PositionId,
                        ItemPositionId = pricePosition.PositionId,

                        // у пакетных позиций нет понятия объекта привязки
                        FirmAddressId = null,
                        CategoryId = null,
                    };

            // объединяем пакетные и не-пакетные позиции в одну мега-таблицу
            // в колонках PackagePositionId\ItemPositionId будут как id пакетов так и id простых позиций
            // в дальнейшем это позволит легко проверить запрещение пакета и не-пакета 
            return opas.Union(pkgs);
        }

        public FindSpecification<OrderItem> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<OrderItem>.Contains(x => x.OrderPositionId, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<OrderItem> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<OrderItem> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<OrderItem> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<OrderItem> dataObjects)
        {
            var orderIds = dataObjects.Select(x => x.OrderId).ToHashSet();

            var firmIds = _query.For<Order>()
                .Where(x => orderIds.Contains(x.Id))
                .Select(x => x.FirmId)
                .Distinct()
                .ToList();

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(OrderItem), typeof(Firm), firmIds)};
        }
    }
}