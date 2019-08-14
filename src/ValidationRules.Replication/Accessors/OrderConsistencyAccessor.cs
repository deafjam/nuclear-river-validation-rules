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
    public sealed class OrderConsistencyAccessor : IStorageBasedDataObjectAccessor<OrderConsistency>, IDataChangesHandler<OrderConsistency>
    {
        private readonly IQuery _query;

        public OrderConsistencyAccessor(IQuery query) => _query = query;

        public IQueryable<OrderConsistency> GetSource() =>
            from order in _query.For(Specs.Find.Erm.Order)
            select new OrderConsistency
            {
                Id = order.Id,
                FirmId = order.FirmId,

                SignupDate = order.SignupDate,
                LegalPersonId = order.LegalPersonId,
                LegalPersonProfileId = order.LegalPersonProfileId,
                BranchOfficeOrganizationUnitId = order.BranchOfficeOrganizationUnitId,
                BargainId = order.BargainId,
                DealId = order.DealId,

                IsFreeOfCharge = Erm::Order.FreeOfChargeTypes.Contains(order.OrderType),
                HasCurrency = order.CurrencyId != null,
            };

        public FindSpecification<OrderConsistency> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<OrderConsistency>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<OrderConsistency> dataObjects) => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<OrderConsistency> dataObjects) => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<OrderConsistency> dataObjects) => Array.Empty<IEvent>();
        
        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<OrderConsistency> dataObjects)
        {
            var orderIds = dataObjects.Select(x => x.Id).ToHashSet();
            
            var accountIds =
                (from order in _query.For<OrderConsistency>().Where(x => orderIds.Contains(x.Id))
                from account in _query.For<Account>().Where(x => x.LegalPersonId == order.LegalPersonId && x.BranchOfficeOrganizationUnitId == order.BranchOfficeOrganizationUnitId)
                select account.Id)
                .Distinct()
                .ToList();
            
            return new IEvent[]
            {
                new RelatedDataObjectOutdatedEvent(typeof(OrderConsistency), typeof(Order), orderIds),
                new RelatedDataObjectOutdatedEvent(typeof(OrderConsistency), typeof(Account), accountIds),
            };
        }
    }
}