using System.Collections.Generic;
using System.Linq;
using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules;
using NuClear.ValidationRules.Storage.Model.Messages;

using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using Order = NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules.Order;

namespace NuClear.ValidationRules.Replication.FirmRules.Aggregates
{
    public sealed class OrderAggregateRootActor : AggregateRootActor<Order>
    {
        public OrderAggregateRootActor(
            IQuery query,
            IEqualityComparerFactory equalityComparerFactory,
            IBulkRepository<Order> bulkRepository,
            IBulkRepository<Order.FirmOrganizationUnitMismatch> invalidFirmRepository,
            IBulkRepository<Order.InvalidFirm> orderInvalidFirmRepository,
            IBulkRepository<Order.PartnerPosition> partnerPositionRepository,
            IBulkRepository<Order.PremiumPartnerPosition> premiumPartnerPositionRepository,
            IBulkRepository<Order.FmcgCutoutPosition> fmcgCutoutPositionRepository)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new OrderAccessor(query),
                          bulkRepository,
                          HasValueObject(new OrderFirmOrganizationUnitMismatchAccessor(query), invalidFirmRepository),
                          HasValueObject(new OrderInvalidFirmAccessor(query), orderInvalidFirmRepository),
                          HasValueObject(new PartnerPositionAccessor(query), partnerPositionRepository),
                          HasValueObject(new PremiumPartnerPositionAccessor(query), premiumPartnerPositionRepository),
                          HasValueObject(new FmcgCutoutPositionAccessor(query), fmcgCutoutPositionRepository));
        }

        public sealed class OrderAccessor : DataChangesHandler<Order>, IStorageBasedDataObjectAccessor<Order>
        {
            private readonly IQuery _query;

            public OrderAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.FirmAndOrderShouldBelongTheSameOrganizationUnit, GetRelatedOrders},
                        {MessageTypeCode.FirmShouldHaveLimitedCategoryCount, GetRelatedOrders},
                        {MessageTypeCode.LinkedFirmShouldBeValid, GetRelatedOrders},

                        {MessageTypeCode.FirmAddressMustNotHaveMultiplePremiumPartnerAdvertisement, GetRelatedOrders},
                        {MessageTypeCode.FirmAddressShouldNotHaveMultiplePartnerAdvertisement, GetRelatedOrders },
                        {MessageTypeCode.PartnerAdvertisementMustNotCauseProblemsToTheAdvertiser, GetRelatedOrders},
                        {MessageTypeCode.PartnerAdvertisementShouldNotBeSoldToAdvertiser, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order> dataObjects) =>
                dataObjects.Select(x => x.Id);
            
            public IQueryable<Order> GetSource()
                =>
                    from order in _query.For<Facts::Order>() 
                    from orderWorkflow in _query.For<Facts::OrderWorkflow>().Where(x => x.Id == order.Id)
                    select new Order
                    {
                       Id = order.Id,
                       FirmId = order.FirmId,
                       Start = order.AgileDistributionStartDate,
                       End = order.AgileDistributionEndFactDate,
                       Scope = Scope.Compute(orderWorkflow.Step, order.Id),
                    };

            public FindSpecification<Order> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
                return new FindSpecification<Order>(x => aggregateIds.Contains(x.Id));
            }
        }

        public sealed class OrderFirmOrganizationUnitMismatchAccessor : DataChangesHandler<Order.FirmOrganizationUnitMismatch>, IStorageBasedDataObjectAccessor<Order.FirmOrganizationUnitMismatch>
        {
            private readonly IQuery _query;

            public OrderFirmOrganizationUnitMismatchAccessor(IQuery query) :base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.FirmAndOrderShouldBelongTheSameOrganizationUnit, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.FirmOrganizationUnitMismatch> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.FirmOrganizationUnitMismatch> GetSource()
                => from order in _query.For<Facts::Order>()
                   from firm in _query.For<Facts::Firm>().Where(x => x.Id == order.FirmId)
                   where order.ProjectId != firm.ProjectId
                   select new Order.FirmOrganizationUnitMismatch { OrderId = order.Id };

            public FindSpecification<Order.FirmOrganizationUnitMismatch> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.FirmOrganizationUnitMismatch>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class PartnerPositionAccessor : DataChangesHandler<Order.PartnerPosition>, IStorageBasedDataObjectAccessor<Order.PartnerPosition>
        {
            private readonly IQuery _query;

            public PartnerPositionAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.FirmAddressMustNotHaveMultiplePremiumPartnerAdvertisement, GetRelatedOrders},
                        {MessageTypeCode.FirmAddressShouldNotHaveMultiplePartnerAdvertisement, GetRelatedOrders },
                        {MessageTypeCode.PartnerAdvertisementMustNotCauseProblemsToTheAdvertiser, GetRelatedOrders},
                        {MessageTypeCode.PartnerAdvertisementShouldNotBeSoldToAdvertiser, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.PartnerPosition> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.PartnerPosition> GetSource()
            {
                var addressPositions =
                    from position in _query.For<Facts::Position>().Where(x => x.CategoryCode == Facts::Position.CategoryCodePartnerAdvertisingAddress)
                    from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.FirmAddressId.HasValue).Where(x => x.PositionId == position.Id)
                    from fa in _query.For<Facts::FirmAddress>().Where(x => x.Id == opa.FirmAddressId.Value)
                    select new Order.PartnerPosition
                    {
                        OrderId = opa.OrderId,
                        OrderPositionId = opa.OrderPositionId, 
                        DestinationFirmAddressId = opa.FirmAddressId.Value,
                        DestinationFirmId = fa.FirmId,
                    };

                return addressPositions;
            }

            public FindSpecification<Order.PartnerPosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.PartnerPosition>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class PremiumPartnerPositionAccessor : DataChangesHandler<Order.PremiumPartnerPosition>, IStorageBasedDataObjectAccessor<Order.PremiumPartnerPosition>
        {
            private readonly IQuery _query;

            public PremiumPartnerPositionAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator() => new RuleInvalidator
            {
                {MessageTypeCode.FirmAddressMustNotHaveMultiplePremiumPartnerAdvertisement, GetRelatedOrders},
                {MessageTypeCode.FirmAddressShouldNotHaveMultiplePartnerAdvertisement, GetRelatedOrders },
            };
            
            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.PremiumPartnerPosition> dataObjects) =>
                dataObjects.Select(x => x.OrderId);

            public IQueryable<Order.PremiumPartnerPosition> GetSource()
            {
                var ordersWithPremium =
                    (from position in _query.For<Facts::Position>().Where(x => Facts::Position.CategoryCodesCategoryCodePremiumPartnerAdvertising.Contains(x.CategoryCode))
                    from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.PositionId == position.Id)
                    select new Order.PremiumPartnerPosition
                    {
                        OrderId = opa.OrderId
                    }).Distinct();

                return ordersWithPremium;
            }

            public FindSpecification<Order.PremiumPartnerPosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.PremiumPartnerPosition>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class FmcgCutoutPositionAccessor : DataChangesHandler<Order.FmcgCutoutPosition>, IStorageBasedDataObjectAccessor<Order.FmcgCutoutPosition>
        {
            private readonly IQuery _query;

            public FmcgCutoutPositionAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.PartnerAdvertisementMustNotCauseProblemsToTheAdvertiser, GetRelatedOrders}
                    };
            
            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.FmcgCutoutPosition> dataObjects) =>
                dataObjects.Select(x => x.OrderId);

            public IQueryable<Order.FmcgCutoutPosition> GetSource()
            {
                var opaPositions =
                    from position in _query.For<Facts::Position>()
                                           .Where(x => x.CategoryCode == Facts::Position.CategoryCodeBasicPackage
                                                       || x.CategoryCode == Facts::Position.CategoryCodeMediaContextBanner
                                                       || x.CategoryCode == Facts::Position.CategoryCodeContextBanner)
                    from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.PositionId == position.Id)
                    select new Order.FmcgCutoutPosition
                        {
                            OrderId = opa.OrderId,
                        };

                var pricePositions =
                    from position in _query.For<Facts::Position>()
                                           .Where(x => x.CategoryCode == Facts::Position.CategoryCodeBasicPackage
                                                       || x.CategoryCode == Facts::Position.CategoryCodeMediaContextBanner
                                                       || x.CategoryCode == Facts::Position.CategoryCodeContextBanner)
                    from pp in _query.For<Facts::PricePosition>().Where(x => x.PositionId == position.Id)
                    from op in _query.For<Facts::OrderPosition>().Where(x => x.PricePositionId == pp.Id)
                    select new Order.FmcgCutoutPosition
                        {
                            OrderId = op.OrderId,
                        };

                var result = opaPositions.Union(pricePositions); 
                return result;
            }

            public FindSpecification<Order.FmcgCutoutPosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.FmcgCutoutPosition>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class OrderInvalidFirmAccessor : DataChangesHandler<Order.InvalidFirm>, IStorageBasedDataObjectAccessor<Order.InvalidFirm>
        {
            private readonly IQuery _query;

            public OrderInvalidFirmAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.LinkedFirmShouldBeValid, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.InvalidFirm> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.InvalidFirm> GetSource()
            {
                var result =
                    (from order in _query.For<Facts::Order>()
                    from fInactive in _query.For<Facts::FirmInactive>().Where(x => x.Id == order.FirmId)
                        .DefaultIfEmpty()
                    let state = fInactive == default ? InvalidFirmState.NotSet
                        : fInactive.IsDeleted ? InvalidFirmState.Deleted
                        : !fInactive.IsActive ? InvalidFirmState.ClosedForever
                        : fInactive.IsClosedForAscertainment ? InvalidFirmState.ClosedForAscertainment
                        : InvalidFirmState.NotSet
                    from fa in _query.For<Facts::FirmAddress>().Where(x => x.FirmId == order.FirmId).DefaultIfEmpty()
                    let state2 = fa == default
                        ? InvalidFirmState.HasNoAddresses
                        : InvalidFirmState.NotSet
                    where state != InvalidFirmState.NotSet || state2 != InvalidFirmState.NotSet
                    select new Order.InvalidFirm
                    {
                        OrderId = order.Id,
                        FirmId = order.FirmId,
                        State = state != InvalidFirmState.NotSet ? state : state2,
                    }).Distinct();

                return result;
            }

            public FindSpecification<Order.InvalidFirm> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.InvalidFirm>(x => aggregateIds.Contains(x.OrderId));
            }
        }
    }
}
