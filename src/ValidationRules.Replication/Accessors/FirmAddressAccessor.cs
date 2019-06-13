using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Specs;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.Accessors
{
    public sealed class FirmAddressAccessor : IStorageBasedDataObjectAccessor<FirmAddress>, IDataChangesHandler<FirmAddress>
    {
        private readonly IQuery _query;

        public FirmAddressAccessor(IQuery query) => _query = query;

        public IQueryable<FirmAddress> GetSource() => _query
            .For(Specs.Find.Erm.FirmAddress.Active)
            .Select(x => new FirmAddress
            {
                Id = x.Id,
                FirmId = x.FirmId,

                IsLocatedOnTheMap = x.IsLocatedOnTheMap,
                EntranceCode = x.EntranceCode,
                BuildingPurposeCode = x.BuildingPurposeCode
            });

        public FindSpecification<FirmAddress> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().Select(c => c.DataObjectId).ToList();
            return SpecificationFactory<FirmAddress>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<FirmAddress> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<FirmAddress> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<FirmAddress> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<FirmAddress> dataObjects)
        {
            var firmIds = dataObjects.Select(x => x.FirmId);
            var orderIdsByFirm =
                from order in _query.For<Order>().Where(x => firmIds.Contains(x.FirmId))
                select order.Id;

            var firmAddressIds = dataObjects.Select(x => x.Id);
            var orderIdsByUsage =
                from opa in _query.For<OrderPositionAdvertisement>().Where(x => firmAddressIds.Contains(x.FirmAddressId.Value))
                from op in _query.For<OrderPosition>().Where(x => x.Id == opa.OrderPositionId)
                select op.OrderId;

            return new EventCollectionHelper<FirmAddress> { { typeof(Order), orderIdsByFirm.Union(orderIdsByUsage) } };
        }
    }

    public sealed class FirmAddressInactiveAccessor : IStorageBasedDataObjectAccessor<FirmAddressInactive>, IDataChangesHandler<FirmAddressInactive>
    {
        private readonly IQuery _query;

        public FirmAddressInactiveAccessor(IQuery query) => _query = query;

        public IQueryable<FirmAddressInactive> GetSource() => _query
            .For(Specs.Find.Erm.FirmAddress.Inactive)
            .Select(x => new FirmAddressInactive
            {
                Id = x.Id,

                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                IsClosedForAscertainment = x.ClosedForAscertainment,
            });

        public FindSpecification<FirmAddressInactive> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().Select(c => c.DataObjectId).ToList();
            return SpecificationFactory<FirmAddressInactive>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<FirmAddressInactive> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<FirmAddressInactive> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<FirmAddressInactive> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<FirmAddressInactive> dataObjects)
        {
            var firmAddressIds = dataObjects.Select(x => x.Id);
            var orderIdsByUsage =
                from opa in _query.For<OrderPositionAdvertisement>().Where(x => firmAddressIds.Contains(x.FirmAddressId.Value))
                from op in _query.For<OrderPosition>().Where(x => x.Id == opa.OrderPositionId)
                select op.OrderId;

            return new EventCollectionHelper<FirmAddress> { { typeof(Order), orderIdsByUsage } };
        }
    }
}