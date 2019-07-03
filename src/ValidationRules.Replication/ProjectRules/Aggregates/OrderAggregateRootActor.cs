using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Storage.Model.Aggregates.ProjectRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.ProjectRules.Aggregates
{
    public sealed class OrderAggregateRootActor : AggregateRootActor<Order>
    {
        public OrderAggregateRootActor(
            IQuery query,
            IEqualityComparerFactory equalityComparerFactory,
            IBulkRepository<Order> bulkRepository,
            IBulkRepository<Order.AddressAdvertisementNonOnTheMap> addressAdvertisementRepository,
            IBulkRepository<Order.CategoryAdvertisement> categoryAdvertisementRepository,
            IBulkRepository<Order.CostPerClickAdvertisement> costPerClickAdvertisementRepository)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new OrderAccessor(query), bulkRepository,
               HasValueObject(new AddressAdvertisementNonOnTheMapAccessor(query), addressAdvertisementRepository),
               HasValueObject(new CategoryAdvertisementAccessor(query), categoryAdvertisementRepository),
               HasValueObject(new CostPerClickAdvertisementAccessor(query), costPerClickAdvertisementRepository));
        }

        public sealed class OrderAccessor : DataChangesHandler<Order>, IStorageBasedDataObjectAccessor<Order>
        {
            private readonly IQuery _query;

            public OrderAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.FirmAddressMustBeLocatedOnTheMap, GetRelatedOrders},
                        {MessageTypeCode.OrderMustUseCategoriesOnlyAvailableInProject, GetRelatedOrders},
                        {MessageTypeCode.OrderPositionCostPerClickMustBeSpecified, GetRelatedOrders},
                        {MessageTypeCode.OrderPositionCostPerClickMustNotBeLessMinimum, GetRelatedOrders},
                        {MessageTypeCode.OrderPositionSalesModelMustMatchCategorySalesModel, GetRelatedOrders},
                        {MessageTypeCode.ProjectMustContainCostPerClickMinimumRestriction, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order> dataObjects) =>
                dataObjects.Select(x => x.Id);
            
            public IQueryable<Order> GetSource()
                => from order in _query.For<Facts::Order>()
                   from project in _query.For<Facts::Project>().Where(x => x.OrganizationUnitId == order.DestOrganizationUnitId)
                   select new Order
                       {
                           Id = order.Id,
                           Start = order.AgileDistributionStartDate,
                           End = order.AgileDistributionEndPlanDate, // ?
                           ProjectId = project.Id,
                           IsDraft = order.WorkflowStep == Facts::Order.State.OnRegistration,
                       };

            public FindSpecification<Order> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
                return new FindSpecification<Order>(x => aggregateIds.Contains(x.Id));
            }
        }

        public sealed class AddressAdvertisementNonOnTheMapAccessor : DataChangesHandler<Order.AddressAdvertisementNonOnTheMap>, IStorageBasedDataObjectAccessor<Order.AddressAdvertisementNonOnTheMap>
        {
            private readonly IQuery _query;

            public AddressAdvertisementNonOnTheMapAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.FirmAddressMustBeLocatedOnTheMap, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.AddressAdvertisementNonOnTheMap> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.AddressAdvertisementNonOnTheMap> GetSource()
                => (from op in _query.For<Facts::OrderPosition>()
                    from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.FirmAddressId.HasValue).Where(x => x.OrderPositionId == op.Id)
                    from position in _query.For<Facts::Position>()
                                           .Where(x => !x.IsDeleted && !Facts::Position.CategoryCodesAllowNotLocatedOnTheMap.Contains(x.CategoryCode))
                                           .Where(x => x.Id == opa.PositionId)
                    from firmAddress in _query.For<Facts::FirmAddress>().Where(x => !x.IsLocatedOnTheMap).Where(x => x.Id == opa.FirmAddressId.Value)
                    select new Order.AddressAdvertisementNonOnTheMap
                        {
                            OrderId = op.OrderId,
                            OrderPositionId = op.Id,
                            PositionId = opa.PositionId,
                            AddressId = opa.FirmAddressId.Value,
                        }).Distinct();

        public FindSpecification<Order.AddressAdvertisementNonOnTheMap> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.AddressAdvertisementNonOnTheMap>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class CategoryAdvertisementAccessor : DataChangesHandler<Order.CategoryAdvertisement>, IStorageBasedDataObjectAccessor<Order.CategoryAdvertisement>
        {
            private readonly IQuery _query;

            public CategoryAdvertisementAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.OrderMustUseCategoriesOnlyAvailableInProject, GetRelatedOrders},
                        {MessageTypeCode.OrderPositionCostPerClickMustBeSpecified, GetRelatedOrders},
                        {MessageTypeCode.OrderPositionSalesModelMustMatchCategorySalesModel, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.CategoryAdvertisement> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.CategoryAdvertisement> GetSource()
                => (from op in _query.For<Facts::OrderPosition>()
                    from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == op.Id)
                    from position in _query.For<Facts::Position>().Where(x => !x.IsDeleted).Where(x => x.Id == opa.PositionId)
                    from category in _query.For<Facts::Category>().Where(x => x.IsActiveNotDeleted).Where(x => x.Id == opa.CategoryId.Value)
                    where opa.CategoryId.HasValue
                    select new Order.CategoryAdvertisement
                        {
                            OrderId = op.OrderId,
                            OrderPositionId = op.Id,
                            PositionId = opa.PositionId,
                            CategoryId = opa.CategoryId.Value,
                            SalesModel = position.SalesModel,
                            IsSalesModelRestrictionApplicable = category.L3Id != null && position.PositionsGroup != Facts::Position.PositionsGroupMedia
                    }).Distinct();

            public FindSpecification<Order.CategoryAdvertisement> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.CategoryAdvertisement>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class CostPerClickAdvertisementAccessor : DataChangesHandler<Order.CostPerClickAdvertisement>, IStorageBasedDataObjectAccessor<Order.CostPerClickAdvertisement>
        {
            private readonly IQuery _query;

            public CostPerClickAdvertisementAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.OrderPositionCostPerClickMustBeSpecified, GetRelatedOrders},
                        {MessageTypeCode.OrderPositionCostPerClickMustNotBeLessMinimum, GetRelatedOrders},
                        {MessageTypeCode.ProjectMustContainCostPerClickMinimumRestriction, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.CostPerClickAdvertisement> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.CostPerClickAdvertisement> GetSource()
                => from op in _query.For<Facts::OrderPosition>()
                   from pricePosition in _query.For<Facts::PricePosition>().Where(x => x.Id == op.PricePositionId)
                   from cpc in _query.For<Facts::OrderPositionCostPerClick>().Where(x => x.OrderPositionId == op.Id)
                   select new Order.CostPerClickAdvertisement
                       {
                           OrderId = op.OrderId,
                           OrderPositionId = op.Id,
                           PositionId = pricePosition.PositionId,
                           CategoryId = cpc.CategoryId,
                           Bid = cpc.Amount,
                       };

            public FindSpecification<Order.CostPerClickAdvertisement> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.CostPerClickAdvertisement>(x => aggregateIds.Contains(x.OrderId));
            }
        }
    }
}
