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
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
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
            var firmAddressIds = dataObjects.Select(x => x.Id).ToHashSet();
            
            var orderIds = _query.For<OrderPositionAdvertisement>()
                .Where(x => firmAddressIds.Contains(x.FirmAddressId.Value))
                .Select(x => x.OrderId)
                .Distinct();

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(FirmAddress), typeof(Order), orderIds.ToList())};
        }
    }
}