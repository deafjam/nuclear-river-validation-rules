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
    public sealed class BillAccessor : IStorageBasedDataObjectAccessor<Bill>, IDataChangesHandler<Bill>
    {
        private readonly IQuery _query;

        public BillAccessor(IQuery query) => _query = query;

        public IQueryable<Bill> GetSource() =>
            // join тут можно использовать, т.к. Bill это ValueObject для Order
            from order in _query.For(Specs.Find.Erm.Order)
            from bill in _query.For<Erm::Bill>().Where(x => x.IsActive && !x.IsDeleted && x.BillType == Erm::Bill.Payment).Where(x => x.OrderId == order.Id)
            select new Bill
            {
                Id = bill.Id,
                OrderId = bill.OrderId,
                PayablePlan = bill.PayablePlan,
            };

        public FindSpecification<Bill> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<Bill>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<Bill> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<Bill> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<Bill> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<Bill> dataObjects)
        {
            var orderIds = dataObjects.Select(x => x.OrderId);

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(Bill), typeof(Order), orderIds.ToHashSet())};
        }
    }
}