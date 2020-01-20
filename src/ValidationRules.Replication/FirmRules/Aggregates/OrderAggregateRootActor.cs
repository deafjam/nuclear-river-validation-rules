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
            IBulkRepository<Order.FmcgCutoutPosition> fmcgCutoutPositionRepository,
            IBulkRepository<Order.AddressAdvertisementNonOnTheMap> addressAdvertisementRepository,
            IBulkRepository<Order.MissingValidPartnerFirmAddresses> missingValidPartnerFirmAddressesRepository,
            IBulkRepository<Order.InvalidFirmAddress> orderInvalidFirmAddressRepository,
            IBulkRepository<Order.InvalidCategory> invalidCategoryRepository,
            IBulkRepository<Order.CategoryNotBelongsToAddress> categoryNotBelongsToAddress)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new OrderAccessor(query),
                          bulkRepository,
                          HasValueObject(new OrderFirmOrganizationUnitMismatchAccessor(query), invalidFirmRepository),
                          HasValueObject(new OrderInvalidFirmAccessor(query), orderInvalidFirmRepository),
                          HasValueObject(new PartnerPositionAccessor(query), partnerPositionRepository),
                          HasValueObject(new PremiumPartnerPositionAccessor(query), premiumPartnerPositionRepository),
                          HasValueObject(new FmcgCutoutPositionAccessor(query), fmcgCutoutPositionRepository),
                          HasValueObject(new AddressAdvertisementNonOnTheMapAccessor(query), addressAdvertisementRepository),
                          HasValueObject(new MissingValidPartnerFirmAddressesAccessor(query), missingValidPartnerFirmAddressesRepository),
                          HasValueObject(new InvalidFirmAddressAccessor(query), orderInvalidFirmAddressRepository),
                          HasValueObject(new InvalidCategoryAccessor(query), invalidCategoryRepository),
                          HasValueObject(new CategoryNotBelongsToAddressAccessor(query), categoryNotBelongsToAddress));
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
                        
                        {MessageTypeCode.FirmAddressMustBeLocatedOnTheMap, GetRelatedOrders},
                        {MessageTypeCode.AtLeastOneLinkedPartnerFirmAddressShouldBeValid, GetRelatedOrders},
                        {MessageTypeCode.LinkedFirmAddressShouldBeValid, GetRelatedOrders},
                        {MessageTypeCode.LinkedCategoryAsteriskMayBelongToFirm, GetRelatedOrders},
                        {MessageTypeCode.LinkedCategoryShouldBeActive, GetRelatedOrders},
                        {MessageTypeCode.LinkedCategoryShouldBelongToFirm, GetRelatedOrders},
                        {MessageTypeCode.LinkedCategoryFirmAddressShouldBeValid, GetRelatedOrders},
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
                    (from position in _query.For<Facts::Position>().Where(x => Facts::Position.CategoryCodesPremiumPartnerAdvertising.Contains(x.CategoryCode))
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
                                           .Where(x => Facts::Position.CategoryCodesFmcgCutout.Contains(x.CategoryCode))
                    from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.PositionId == position.Id)
                    select new Order.FmcgCutoutPosition
                        {
                            OrderId = opa.OrderId,
                        };

                var pricePositions =
                    from position in _query.For<Facts::Position>()
                                           .Where(x => Facts::Position.CategoryCodesFmcgCutout.Contains(x.CategoryCode))
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
            {
                var result = 
                    (
                    from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.FirmAddressId.HasValue)
                    from position in _query.For<Facts::Position>()
                        .Where(x => !x.IsDeleted &&
                                    !Facts::Position.CategoryCodesAllowNotLocatedOnTheMap.Contains(x.CategoryCode))
                        .Where(x => x.Id == opa.PositionId)
                    from firmAddress in _query.For<Facts::FirmAddress>().Where(x => !x.IsLocatedOnTheMap)
                        .Where(x => x.Id == opa.FirmAddressId.Value)
                    select new Order.AddressAdvertisementNonOnTheMap
                    {
                        OrderId = opa.OrderId,
                        OrderPositionId = opa.OrderPositionId,
                        PositionId = opa.PositionId,
                        AddressId = opa.FirmAddressId.Value,
                    }).Distinct();

                return result;
            }

            public FindSpecification<Order.AddressAdvertisementNonOnTheMap> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.AddressAdvertisementNonOnTheMap>(x => aggregateIds.Contains(x.OrderId));
            }
        }
        
        public sealed class MissingValidPartnerFirmAddressesAccessor : DataChangesHandler<Order.MissingValidPartnerFirmAddresses>, IStorageBasedDataObjectAccessor<Order.MissingValidPartnerFirmAddresses>
        {
            private readonly IQuery _query;

            public MissingValidPartnerFirmAddressesAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.AtLeastOneLinkedPartnerFirmAddressShouldBeValid, GetRelatedOrders}
                    };
            
            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.MissingValidPartnerFirmAddresses> dataObjects) =>
                dataObjects.Select(x => x.OrderId);

            public IQueryable<Order.MissingValidPartnerFirmAddresses> GetSource()
            {
                var query =
                    from op in _query.For<Facts::OrderPosition>()
                    from pp in _query.For<Facts::PricePosition>().Where(x => x.Id == op.PricePositionId)
                    let hasPartnerPosition =
                        (from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == op.Id)
                        from p in _query.For<Facts::Position>().Where(x => x.CategoryCode == Facts::Position.CategoryCodePartnerAdvertisingAddress).Where(x => x.Id == opa.PositionId)
                        select opa.OrderPositionId).Any()

                    let hasValidAddress =
                        (from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == op.Id)
                        from fa in _query.For<Facts::FirmAddress>().Where(x => x.Id == opa.FirmAddressId)
                        select opa.OrderPositionId).Any()

                    where hasPartnerPosition && !hasValidAddress

                    select new Order.MissingValidPartnerFirmAddresses
                    {
                        OrderId = op.OrderId,
                        OrderPositionId = op.Id,
                        PositionId = pp.PositionId
                    };

                return query;
            }

            public FindSpecification<Order.MissingValidPartnerFirmAddresses> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.MissingValidPartnerFirmAddresses>(x => aggregateIds.Contains(x.OrderId));
            }
        }
        
        public sealed class InvalidFirmAddressAccessor : DataChangesHandler<Order.InvalidFirmAddress>, IStorageBasedDataObjectAccessor<Order.InvalidFirmAddress>
        {
            private readonly IQuery _query;

            public InvalidFirmAddressAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.LinkedFirmAddressShouldBeValid, GetRelatedOrders},
                    };
            
            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.InvalidFirmAddress> dataObjects) =>
                dataObjects.Select(x => x.OrderId);

            public IQueryable<Order.InvalidFirmAddress> GetSource()
            {
                var result =
                    from order in _query.For<Facts::Order>()
                    from opa in _query.For<Facts::OrderPositionAdvertisement>()
                        .Where(x => x.OrderId == order.Id)
                    from position in _query.For<Facts::Position>().Where(x => x.Id == opa.PositionId)
                    let isPartnerAddress =
                        Facts::Position.CategoryCodesAllowFirmMismatch.Contains(position.CategoryCode) &&
                        position.BindingObjectType == Facts::Position.BindingObjectTypeAddressMultiple
                    let checkPoi = Facts::Position.CategoryCodesPoiAddressCheck.Contains(position.CategoryCode)
                    from faInactive in _query.For<Facts::FirmAddressInactive>()
                        .Where(x => x.Id == opa.FirmAddressId.Value).DefaultIfEmpty()
                    let state = faInactive == default ? InvalidFirmAddressState.NotSet
                        : faInactive.IsDeleted ? InvalidFirmAddressState.Deleted
                        : !faInactive.IsActive ? InvalidFirmAddressState.NotActive
                        : faInactive.IsClosedForAscertainment ? InvalidFirmAddressState.ClosedForAscertainment
                        : InvalidFirmAddressState.NotSet
                    from fa in _query.For<Facts::FirmAddress>().Where(x => x.Id == opa.FirmAddressId.Value)
                        .DefaultIfEmpty()
                    let state2 = fa == default
                        ? InvalidFirmAddressState.NotSet
                        : fa.FirmId != order.FirmId && !isPartnerAddress
                            ? InvalidFirmAddressState.NotBelongToFirm
                            : checkPoi && fa.BuildingPurposeCode.HasValue &&
                              Facts::FirmAddress.InvalidBuildingPurposeCodesForPoi.Contains(
                                  fa.BuildingPurposeCode.Value)
                                ? InvalidFirmAddressState.InvalidBuildingPurpose
                                : checkPoi && fa.EntranceCode == null
                                    ? InvalidFirmAddressState.MissingEntrance
                                    : InvalidFirmAddressState.NotSet
                    where state != InvalidFirmAddressState.NotSet || state2 != InvalidFirmAddressState.NotSet
                    select new Order.InvalidFirmAddress
                    {
                        OrderId = opa.OrderId,
                        FirmAddressId = opa.FirmAddressId.Value,
                        OrderPositionId = opa.OrderPositionId,
                        PositionId = opa.PositionId,
                        State = state != InvalidFirmAddressState.NotSet ? state : state2,
                        IsPartnerAddress = isPartnerAddress
                    };

                return result.Distinct();
            }

            public FindSpecification<Order.InvalidFirmAddress> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.InvalidFirmAddress>(x => aggregateIds.Contains(x.OrderId));
            }
        }
        
        public sealed class InvalidCategoryAccessor : DataChangesHandler<Order.InvalidCategory>, IStorageBasedDataObjectAccessor<Order.InvalidCategory>
        {
            private readonly IQuery _query;

            public InvalidCategoryAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.LinkedCategoryAsteriskMayBelongToFirm, GetRelatedOrders},
                        {MessageTypeCode.LinkedCategoryShouldBeActive, GetRelatedOrders},
                        {MessageTypeCode.LinkedCategoryShouldBelongToFirm, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.InvalidCategory> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.InvalidCategory> GetSource()
            {
                var result = 
                    from order in _query.For<Facts::Order>()
                    from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.CategoryId.HasValue).Where(x => x.OrderId == order.Id)
                    from category in _query.For<Facts::Category>().Where(x => x.Id == opa.CategoryId.Value)
                    from position in _query.For<Facts::Position>().Where(x => !x.IsDeleted).Where(x => x.Id == opa.PositionId)
                    let categoryBelongToFirm = (from fa in _query.For<Facts::FirmAddress>().Where(x => x.FirmId == order.FirmId)
                                                from cfa in _query.For<Facts::FirmAddressCategory>().Where(x => x.FirmAddressId == fa.Id && x.CategoryId == opa.CategoryId.Value)
                                                select fa.Id).Any()
                    let state = !category.IsActiveNotDeleted ? InvalidCategoryState.Inactive
                        : !categoryBelongToFirm ? InvalidCategoryState.NotBelongToFirm
                        : InvalidCategoryState.NotSet
                    where state != InvalidCategoryState.NotSet
                    select new Order.InvalidCategory
                        {
                            OrderId = opa.OrderId,
                            CategoryId = opa.CategoryId.Value,
                            OrderPositionId = opa.OrderPositionId,
                            PositionId = opa.PositionId,
                            MayNotBelongToFirm = position.BindingObjectType == Facts::Position.BindingObjectTypeCategoryMultipleAsterisk,
                            State = state,
                        };

                return result;
            }

            public FindSpecification<Order.InvalidCategory> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.InvalidCategory>(x => aggregateIds.Contains(x.OrderId));
            }
        }
        
        public sealed class CategoryNotBelongsToAddressAccessor : DataChangesHandler<Order.CategoryNotBelongsToAddress>, IStorageBasedDataObjectAccessor<Order.CategoryNotBelongsToAddress>
        {
            private readonly IQuery _query;

            public CategoryNotBelongsToAddressAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.LinkedCategoryFirmAddressShouldBeValid, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.CategoryNotBelongsToAddress> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.CategoryNotBelongsToAddress> GetSource()
            {
                var result =
                    from opa in _query.For<Facts::OrderPositionAdvertisement>()
                        .Where(x => x.FirmAddressId != null && x.CategoryId != null)
                    from cfa in _query.For<Facts::FirmAddressCategory>().Where(x =>
                            x.FirmAddressId == opa.FirmAddressId.Value && x.CategoryId == opa.CategoryId.Value)
                        .DefaultIfEmpty()
                    where cfa == null
                    select new Order.CategoryNotBelongsToAddress
                    {
                        OrderId = opa.OrderId,
                        FirmAddressId = opa.FirmAddressId.Value,
                        CategoryId = opa.CategoryId.Value,
                        OrderPositionId = opa.OrderPositionId,
                        PositionId = opa.PositionId,
                    };

                return result;
            }

            public FindSpecification<Order.CategoryNotBelongsToAddress> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.CategoryNotBelongsToAddress>(x => aggregateIds.Contains(x.OrderId));
            }
        }
    }
}
