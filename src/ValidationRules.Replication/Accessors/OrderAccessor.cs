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
    public sealed class OrderAccessor : IStorageBasedDataObjectAccessor<Order>, IDataChangesHandler<Order>
    {
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);
        private readonly IQuery _query;

        public OrderAccessor(IQuery query) => _query = query;

        public IQueryable<Order> GetSource() => _query
            .For(Specs.Find.Erm.Order)
            .Select(order => new Order
            {
                Id = order.Id,
                FirmId = order.FirmId,

                AgileDistributionStartDate = order.AgileDistributionStartDate,
                AgileDistributionEndPlanDate = order.AgileDistributionEndPlanDate + OneSecond,
                AgileDistributionEndFactDate = order.AgileDistributionEndFactDate + OneSecond,

                SignupDate = order.SignupDate,

                DestOrganizationUnitId = order.DestOrganizationUnitId,

                LegalPersonId = order.LegalPersonId,
                LegalPersonProfileId = order.LegalPersonProfileId,
                BranchOfficeOrganizationUnitId = order.BranchOfficeOrganizationUnitId,
                BargainId = order.BargainId,
                DealId = order.DealId,

                WorkflowStep = order.WorkflowStepId,
                IsFreeOfCharge = Erm::Order.FreeOfChargeTypes.Contains(order.OrderType),
                HasCurrency = order.CurrencyId != null,
                IsSelfAds = order.OrderType == Erm::Order.OrderTypeSelfAds,
                IsSelfSale = order.SaleType == Erm::Order.OrderSaleTypeSelfSale,
            });

        public FindSpecification<Order> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<Order>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<Order> dataObjects)
            => new [] {new DataObjectCreatedEvent(typeof(Order), dataObjects.Select(x => x.Id))} ;

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<Order> dataObjects)
            => new [] {new DataObjectUpdatedEvent(typeof(Order), dataObjects.Select(x => x.Id))} ;

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<Order> dataObjects)
            => new [] {new DataObjectDeletedEvent(typeof(Order), dataObjects.Select(x => x.Id))} ;

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<Order> dataObjects)
        {
            var orderIds = dataObjects.Select(x => x.Id).ToHashSet();

            var accountIds =
                from order in _query.For<Order>().Where(x => orderIds.Contains(x.Id))
                from account in _query.For<Account>().Where(x => x.LegalPersonId == order.LegalPersonId && x.BranchOfficeOrganizationUnitId == order.BranchOfficeOrganizationUnitId)
                select account.Id;

            var orderDtos =
                (from order in _query.For<Order>().Where(x => orderIds.Contains(x.Id))
                from project in _query.For<Project>().Where(x => x.OrganizationUnitId == order.DestOrganizationUnitId)
                select new
                {
                    ProjectId = project.Id,
                    order.FirmId,
                    order.AgileDistributionStartDate,
                    order.AgileDistributionEndFactDate,
                    order.AgileDistributionEndPlanDate,
                }).ToList();

            var firmIds = orderDtos.Select(x => x.FirmId);
            var periodKeys =
                  orderDtos.Select(x => new PeriodKey(x.ProjectId, x.AgileDistributionStartDate))
                  .Concat(orderDtos.Select(x => new PeriodKey(x.ProjectId, x.AgileDistributionEndFactDate)))
                  .Concat(orderDtos.Select(x => new PeriodKey(x.ProjectId, x.AgileDistributionEndPlanDate)));

            return new IEvent[]
            {
                new RelatedDataObjectOutdatedEvent(typeof(Order), typeof(Account), accountIds.ToHashSet()),
                new RelatedDataObjectOutdatedEvent(typeof(Order), typeof(Firm), firmIds.ToHashSet()),
                new PeriodKeysOutdatedEvent(periodKeys.ToHashSet()), 
            };
        }
    }
}