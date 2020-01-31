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

namespace NuClear.ValidationRules.Replication.Accessors
{
    public sealed class AdvertisementAccessor : IMemoryBasedDataObjectAccessor<Advertisement>, IDataChangesHandler<Advertisement>
    {
        private readonly IQuery _query;

        public AdvertisementAccessor(IQuery query) => _query = query;

        public IReadOnlyCollection<Advertisement> GetDataObjects(IEnumerable<ICommand> commands)
        {
            var dtos = commands
                .Cast<ReplaceDataObjectCommand>()
                .SelectMany(x => x.Dtos)
                .Cast<AdvertisementDto>()
                .GroupBy(x => x.Id)
                .Select(x => x.OrderByDescending(y => y.Offset).First());

            return dtos.Select(x => new Advertisement
            {
                Id = x.Id,
                FirmId = x.FirmId,
                StateCode = x.StateCode,
                Offset = x.Offset,
            }).ToList();
        }

        public FindSpecification<Advertisement> GetFindSpecification(IEnumerable<ICommand> commands)
        {
            var ids = commands.Cast<ReplaceDataObjectCommand>().SelectMany(x => x.Dtos).Cast<AdvertisementDto>().Select(x => x.Id).ToHashSet();

            return new FindSpecification<Advertisement>(x => ids.Contains(x.Id));
        }

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<Advertisement> dataObjects)
        {
            var advertisementIds = dataObjects.Select(x => x.Id);

            var orderIds = _query.For<OrderPositionAdvertisement>()
                .Where(x => advertisementIds.Contains(x.AdvertisementId.Value))
                .Select(x => x.OrderId)
                .Distinct()
                .ToList();

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(Advertisement), typeof(Order), orderIds)};
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<Advertisement> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<Advertisement> dataObjects) => throw new NotSupportedException();
        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<Advertisement> dataObjects) => throw new NotSupportedException();
    }
}
