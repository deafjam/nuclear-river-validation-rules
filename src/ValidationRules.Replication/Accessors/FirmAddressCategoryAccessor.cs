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
    public sealed class FirmAddressCategoryAccessor : IStorageBasedDataObjectAccessor<FirmAddressCategory>, IDataChangesHandler<FirmAddressCategory>
    {
        private readonly IQuery _query;

        public FirmAddressCategoryAccessor(IQuery query) => _query = query;

        public IQueryable<FirmAddressCategory> GetSource() =>
            GetLevelThreeSource().Union(GetLevelOneSource());

        private IQueryable<FirmAddressCategory> GetLevelThreeSource() =>
            // join тут можно использовать, т.к. CategoryFirmAddress это ValueObject для FirmAddress(Active)
            from firmAddress in _query.For(Specs.Find.Erm.FirmAddress.Active)
            from cfa in _query.For<Erm::CategoryFirmAddress>().Where(x => x.IsActive && !x.IsDeleted).Where(x => x.FirmAddressId == firmAddress.Id)
            select new FirmAddressCategory
            {
                FirmAddressId = cfa.FirmAddressId,
                CategoryId = cfa.CategoryId,
            };

        private IQueryable<FirmAddressCategory> GetLevelOneSource()
            => 
                // join тут можно использовать, т.к. CategoryFirmAddress это ValueObject для FirmAddress(Active)
                from firmAddress in _query.For(Specs.Find.Erm.FirmAddress.Active)
                from cfa in _query.For<Erm::CategoryFirmAddress>().Where(x => x.IsActive && !x.IsDeleted).Where(x => x.FirmAddressId == firmAddress.Id)
                from c3 in _query.For<Erm::Category>().Where(x => x.Id == cfa.CategoryId)
                from c2 in _query.For<Erm::Category>().Where(x => x.Id == c3.ParentId)
                select new FirmAddressCategory
                {
                    FirmAddressId = cfa.FirmAddressId,
                    CategoryId = c2.ParentId,
                };

        public FindSpecification<FirmAddressCategory> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<FirmAddressCategory>.Contains(x => x.FirmAddressId, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<FirmAddressCategory> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<FirmAddressCategory> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<FirmAddressCategory> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<FirmAddressCategory> dataObjects)
        {
            var firmAddressIds = dataObjects.Select(x => x.FirmAddressId).ToHashSet();

            var orderIds =
                (from firmAddress in _query.For<FirmAddress>().Where(x => firmAddressIds.Contains(x.Id))
                from order in _query.For<OrderConsistency>().Where(x => x.FirmId == firmAddress.FirmId)
                select order.Id)
                .Distinct()
                .ToList();

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(FirmAddressCategory), typeof(Order), orderIds)};
        }
    }
}