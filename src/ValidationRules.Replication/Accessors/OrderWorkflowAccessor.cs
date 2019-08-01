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
    public sealed class OrderWorkflowAccessor : IStorageBasedDataObjectAccessor<OrderWorkflow>, IDataChangesHandler<OrderWorkflow>
    {
        private readonly IQuery _query;

        public OrderWorkflowAccessor(IQuery query) => _query = query;

        public IQueryable<OrderWorkflow> GetSource() =>
            from order in _query.For(Specs.Find.Erm.Order)
            select new OrderWorkflow
            {
                Id = order.Id,
                Step = order.WorkflowStepId,
            };

        public FindSpecification<OrderWorkflow> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<OrderWorkflow>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<OrderWorkflow> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<OrderWorkflow> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<OrderWorkflow> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<OrderWorkflow> dataObjects)
        {
            var orderIds = dataObjects.Select(x => x.Id).ToHashSet();
            
            var orderDtos =
                (from order in _query.For<OrderConsistency>().Where(x => orderIds.Contains(x.Id))
                 from account in _query.For<Account>().Where(x => x.LegalPersonId == order.LegalPersonId && x.BranchOfficeOrganizationUnitId == order.BranchOfficeOrganizationUnitId)
                 select new { order.FirmId, AccountId = account.Id  })
                .Distinct()
                .ToList();
            
            return new IEvent[]
            {
                new RelatedDataObjectOutdatedEvent(typeof(OrderWorkflow), typeof(Order), orderIds),
                new RelatedDataObjectOutdatedEvent(typeof(OrderWorkflow), typeof(Account), orderDtos.Select(x => x.AccountId).ToHashSet()),
                new RelatedDataObjectOutdatedEvent(typeof(OrderWorkflow), typeof(Firm), orderDtos.Select(x => x.FirmId).ToHashSet()),
            };
        }
    }
}