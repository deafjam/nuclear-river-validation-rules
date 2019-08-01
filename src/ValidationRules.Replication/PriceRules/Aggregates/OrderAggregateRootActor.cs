using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.PriceRules.Aggregates
{
    public sealed class OrderAggregateRootActor : AggregateRootActor<Order>
    {
        public OrderAggregateRootActor(
            IQuery query,
            IEqualityComparerFactory equalityComparerFactory,
            IBulkRepository<Order> bulkRepository,
            IBulkRepository<Order.OrderPeriod> orderPeriodBulkRepository,
            IBulkRepository<Order.OrderPricePosition> orderPricePositionBulkRepository,
            IBulkRepository<Order.OrderCategoryPosition> orderCategoryPositionBulkRepository,
            IBulkRepository<Order.OrderThemePosition> orderThemePositionBulkRepository,
            IBulkRepository<Order.AmountControlledPosition> amountControlledPositionBulkRepository,
            IBulkRepository<Order.ActualPrice> actualPriceBulkRepository,
            IBulkRepository<Order.EntranceControlledPosition> entranceControlledPositionBulkRepository)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new OrderAccessor(query), bulkRepository,
                HasValueObject(new OrderPeriodAccessor(query), orderPeriodBulkRepository),
                HasValueObject(new OrderPricePositionAccessor(query), orderPricePositionBulkRepository),
                HasValueObject(new OrderCategoryPositionAccessor(query), orderCategoryPositionBulkRepository),
                HasValueObject(new OrderThemePositionAccessor(query), orderThemePositionBulkRepository),
                HasValueObject(new AmountControlledPositionAccessor(query), amountControlledPositionBulkRepository),
                HasValueObject(new ActualPriceAccessor(query), actualPriceBulkRepository),
                HasValueObject(new EntranceControlledPositionAccessor(query), entranceControlledPositionBulkRepository));
        }

        public sealed class OrderAccessor : DataChangesHandler<Order>, IStorageBasedDataObjectAccessor<Order>
        {
            private readonly IQuery _query;

            public OrderAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator() => new RuleInvalidator ();
            
            public IQueryable<Order> GetSource()
                =>
                    _query.For<Facts::Order>()
                        .Select(x => new Order
                        {
                            Id = x.Id,
                            FirmId = x.FirmId,
                            Start = x.AgileDistributionStartDate,
                            End = x.AgileDistributionEndPlanDate,
                            IsCommitted = Facts::Order.State.Committed.Contains(x.WorkflowStep)
                        });

            public FindSpecification<Order> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
                return new FindSpecification<Order>(x => aggregateIds.Contains(x.Id));
            }
        }

        public sealed class OrderPeriodAccessor : DataChangesHandler<Order.OrderPeriod>, IStorageBasedDataObjectAccessor<Order.OrderPeriod>
        {
            private readonly IQuery _query;

            public OrderPeriodAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.AdvertisementCountPerCategoryShouldBeLimited, GetRelatedOrders},
                        {MessageTypeCode.AdvertisementCountPerThemeShouldBeLimited, GetRelatedOrders},
                        {MessageTypeCode.AdvertisementAmountShouldMeetMaximumRestrictions, GetRelatedOrders},
                        {MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictionsMass, GetRelatedOrders},
                        {MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions, GetRelatedOrders}
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.OrderPeriod> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.OrderPeriod> GetSource()
                => GetSource1().Concat(GetSource2());

            public IQueryable<Order.OrderPeriod> GetSource1()
                => _query.For<Facts::Order>()
                        .Select(x => new Order.OrderPeriod
                        {
                            OrderId = x.Id,
                            ProjectId = x.DestProjectId,
                            Start = x.AgileDistributionStartDate,
                            End = x.AgileDistributionEndFactDate,
                            Scope = Scope.Compute(x.WorkflowStep, x.Id)
                        });

            public IQueryable<Order.OrderPeriod> GetSource2()
                => _query.For<Facts::Order>()
                        .Where(x => x.AgileDistributionEndFactDate != x.AgileDistributionEndPlanDate)
                        .Select(x => new Order.OrderPeriod
                        {
                            OrderId = x.Id,
                            ProjectId = x.DestProjectId,
                            Start = x.AgileDistributionEndFactDate,
                            End = x.AgileDistributionEndPlanDate,
                            Scope = x.Id
                        });

            public FindSpecification<Order.OrderPeriod> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.OrderPeriod>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class OrderPricePositionAccessor : DataChangesHandler<Order.OrderPricePosition>, IStorageBasedDataObjectAccessor<Order.OrderPricePosition>
        {
            private readonly IQuery _query;

            public OrderPricePositionAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator() => new RuleInvalidator ();

            public IQueryable<Order.OrderPricePosition> GetSource()
                =>
                    from orderPosition in _query.For<Facts::OrderPosition>()
                    join pricePosition in _query.For<Facts::PricePosition>() on orderPosition.PricePositionId equals pricePosition.Id
                    select new Order.OrderPricePosition
                       {
                           OrderId = orderPosition.OrderId,
                           OrderPositionId = orderPosition.Id,
                           PositionId = pricePosition.PositionId,

                           PriceId = pricePosition.PriceId,
                           IsActive = pricePosition.IsActiveNotDeleted,
                       };


            public FindSpecification<Order.OrderPricePosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.OrderPricePosition>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class OrderCategoryPositionAccessor : DataChangesHandler<Order.OrderCategoryPosition>, IStorageBasedDataObjectAccessor<Order.OrderCategoryPosition>
        {
            private readonly IQuery _query;

            public OrderCategoryPositionAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.AdvertisementCountPerCategoryShouldBeLimited, GetRelatedOrders}
                    };
            
            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.OrderCategoryPosition> dataObjects) =>
                dataObjects.Select(x => x.OrderId);

            public IQueryable<Order.OrderCategoryPosition> GetSource()
            {
                var result =
                    from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.CategoryId.HasValue)
                    join position in _query.For<Facts::Position>().Where(x => x.CategoryCode == Facts::Position.CategoryCodeAdvertisementInCategory) on opa.PositionId equals position.Id // join для того, чтобы отбросить неподходящие продажи
                    select new Order.OrderCategoryPosition
                    {
                        OrderId = opa.OrderId,
                        CategoryId = opa.CategoryId.Value,
                    };

                return result;
            }

            public FindSpecification<Order.OrderCategoryPosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.OrderCategoryPosition>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class OrderThemePositionAccessor : DataChangesHandler<Order.OrderThemePosition>, IStorageBasedDataObjectAccessor<Order.OrderThemePosition>
        {
            private readonly IQuery _query;

            public OrderThemePositionAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.AdvertisementCountPerThemeShouldBeLimited, GetRelatedOrders},
                    };
            
            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.OrderThemePosition> dataObjects) =>
                dataObjects.Select(x => x.OrderId);

            public IQueryable<Order.OrderThemePosition> GetSource()
            {
                var result =
                    _query.For<Facts::OrderPositionAdvertisement>()
                    .Where(x => x.ThemeId.HasValue)
                    .Select(x => new Order.OrderThemePosition
                    {
                        OrderId = x.OrderId,
                        ThemeId = x.ThemeId.Value,
                    });

                return result;
            }

            public FindSpecification<Order.OrderThemePosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.OrderThemePosition>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class AmountControlledPositionAccessor : DataChangesHandler<Order.AmountControlledPosition>, IStorageBasedDataObjectAccessor<Order.AmountControlledPosition>
        {
            private readonly IQuery _query;

            public AmountControlledPositionAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.AdvertisementAmountShouldMeetMaximumRestrictions, GetRelatedOrders},
                        {MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictionsMass, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.AmountControlledPosition> dataObjects) =>
                dataObjects.Select(x => x.OrderId);

            public IQueryable<Order.AmountControlledPosition> GetSource()
            {
                var result =
                    from opa in _query.For<Facts::OrderPositionAdvertisement>()
                    join position in _query.For<Facts::Position>().Where(x => !x.IsDeleted && x.IsControlledByAmount) on
                        opa.PositionId equals position.Id
                    select new Order.AmountControlledPosition
                    {
                        OrderId = opa.OrderId,
                        OrderPositionId = opa.OrderPositionId,
                        CategoryCode = position.CategoryCode,
                    };
                
                return result;
            }

            public FindSpecification<Order.AmountControlledPosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.AmountControlledPosition>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class EntranceControlledPositionAccessor : DataChangesHandler<Order.EntranceControlledPosition>, IStorageBasedDataObjectAccessor<Order.EntranceControlledPosition>
        {
            private readonly IQuery _query;

            public EntranceControlledPositionAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions, GetRelatedOrders}
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.EntranceControlledPosition> dataObjects) =>
                dataObjects.Select(x => x.OrderId);

            public IQueryable<Order.EntranceControlledPosition> GetSource()
            {
                var result = 
                    (from opa in _query.For<Facts::OrderPositionAdvertisement>()
                    join position in _query.For<Facts::Position>().Where(x =>
                            Facts.Position.CategoryCodesPoiAddressCheck.Contains(x.CategoryCode)) on opa.PositionId
                        equals
                        position.Id
                    join address in _query.For<Facts::FirmAddress>().Where(x => x.EntranceCode != null) on opa
                        .FirmAddressId equals address.Id
                    select new Order.EntranceControlledPosition
                    {
                        OrderId = opa.OrderId,
                        EntranceCode = address.EntranceCode.Value,
                        FirmAddressId = address.Id,
                    }).Distinct();

                return result;
            }

            public FindSpecification<Order.EntranceControlledPosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.EntranceControlledPosition>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class ActualPriceAccessor : DataChangesHandler<Order.ActualPrice>, IStorageBasedDataObjectAccessor<Order.ActualPrice>
        {
            private readonly IQuery _query;

            public ActualPriceAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator() => new RuleInvalidator ();

            public IQueryable<Order.ActualPrice> GetSource()
            {
                var result =
                    from order in _query.For<Facts::Order>()
                    let price = _query.For<Facts::Price>()
                                .Where(x => x.ProjectId == order.DestProjectId)
                                .Where(x => x.BeginDate <= order.AgileDistributionStartDate)
                                .OrderByDescending(x => x.BeginDate)
                                .FirstOrDefault()
                    select new Order.ActualPrice
                    {
                        OrderId = order.Id,
                        PriceId = price != null ? (long?)price.Id : null
                    };

                return result;
            }

            public FindSpecification<Order.ActualPrice> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.ActualPrice>(x => aggregateIds.Contains(x.OrderId));
            }
        }
    }
}