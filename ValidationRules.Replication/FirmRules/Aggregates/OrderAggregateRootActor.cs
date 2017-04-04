﻿using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Model.FirmRules.Aggregates;
using NuClear.ValidationRules.Storage.Model.Messages;

using Facts = NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.FirmRules.Aggregates
{
    public sealed class OrderAggregateRootActor : AggregateRootActor<Order>
    {
        public OrderAggregateRootActor(
            IQuery query,
            IEqualityComparerFactory equalityComparerFactory,
            IBulkRepository<Order> bulkRepository,
            IBulkRepository<Order.FirmOrganiationUnitMismatch> invalidFirmRepository,
            IBulkRepository<Order.InvalidFirm> orderInvalidFirmRepository,
            IBulkRepository<Order.CategoryPurchase> categoryPurchaseRepository,
            IBulkRepository<Order.NotApplicapleForDesktopPosition> notApplicapleForDesktopPositionRepository,
            IBulkRepository<Order.SelfAdvertisementPosition> selfAdvertisementPositionRepository)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new OrderAccessor(query), bulkRepository,
                HasValueObject(new OrderFirmOrganiationUnitMismatchAccessor(query), invalidFirmRepository),
                HasValueObject(new OrderInvalidFirmAccessor(query), orderInvalidFirmRepository),
                HasValueObject(new NotApplicapleForDesktopPositionAccessor(query), notApplicapleForDesktopPositionRepository),
                HasValueObject(new SelfAdvertisementPositionAccessor(query), selfAdvertisementPositionRepository),
                HasValueObject(new OrderCategoryPurchaseAccessor(query), categoryPurchaseRepository));
        }

        public sealed class OrderAccessor : DataChangesHandler<Order>, IStorageBasedDataObjectAccessor<Order>
        {
            private readonly IQuery _query;

            public OrderAccessor(IQuery query) : base(CreateInvalidator())
            {
                _query = query;
            }

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        MessageTypeCode.FirmAndOrderShouldBelongTheSameOrganizationUnit,
                        MessageTypeCode.FirmShouldHaveLimitedCategoryCount,
                        MessageTypeCode.FirmWithSelfAdvMustHaveOnlyDesktopOrIndependentPositions,
                        MessageTypeCode.FirmWithSpecialCategoryShouldHaveSpecialPurchasesOrder,
                        MessageTypeCode.LinkedFirmShouldBeValid,
                    };

            public IQueryable<Order> GetSource()
                => from order in _query.For<Facts::Order>()
                   select new Order
                       {
                           Id = order.Id,
                           FirmId = order.FirmId,
                           Begin = order.BeginDistribution,
                           End = order.EndDistributionFact,
                           Scope = Scope.Compute(order.WorkflowStep, order.Id),
                       };

            public FindSpecification<Order> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<CreateDataObjectCommand>().Select(c => c.DataObjectId)
                                           .Concat(commands.OfType<SyncDataObjectCommand>().Select(c => c.DataObjectId))
                                           .Concat(commands.OfType<DeleteDataObjectCommand>().Select(c => c.DataObjectId))
                                           .Distinct()
                                           .ToArray();
                return new FindSpecification<Order>(x => aggregateIds.Contains(x.Id));
            }
        }

        public sealed class NotApplicapleForDesktopPositionAccessor : DataChangesHandler<Order.NotApplicapleForDesktopPosition>, IStorageBasedDataObjectAccessor<Order.NotApplicapleForDesktopPosition>
        {
            private const long PlatformIndependent = 0;
            private const long PlatformDesktop = 1;

            private readonly IQuery _query;

            public NotApplicapleForDesktopPositionAccessor(IQuery query) : base(CreateInvalidator())
            {
                _query = query;
            }

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        MessageTypeCode.FirmWithSelfAdvMustHaveOnlyDesktopOrIndependentPositions,
                    };

            public IQueryable<Order.NotApplicapleForDesktopPosition> GetSource()
                => (from order in _query.For<Facts::Order>()
                    from orderPosition in _query.For<Facts::OrderPosition>().Where(x => x.OrderId == order.Id)
                    from orderPositionAdvertisement in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == orderPosition.Id)
                    from position in _query.For<Facts::Position>()
                        .Where(x => !x.IsDeleted)
                        .Where(x => x.Platform != PlatformDesktop && x.Platform != PlatformIndependent)
                        .Where(x => x.Id == orderPositionAdvertisement.PositionId)
                    select new Order.NotApplicapleForDesktopPosition { OrderId = orderPosition.OrderId }).Distinct();

            public FindSpecification<Order.NotApplicapleForDesktopPosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<ReplaceValueObjectCommand>().Select(c => c.AggregateRootId).Distinct().ToArray();
                return new FindSpecification<Order.NotApplicapleForDesktopPosition>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class SelfAdvertisementPositionAccessor : DataChangesHandler<Order.SelfAdvertisementPosition>, IStorageBasedDataObjectAccessor<Order.SelfAdvertisementPosition>
        {
            private const long SelfAdvertisementOnlyOnPc = 287; // Самореклама только для ПК
            private readonly IQuery _query;

            public SelfAdvertisementPositionAccessor(IQuery query) : base(CreateInvalidator())
            {
                _query = query;
            }

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        MessageTypeCode.FirmWithSelfAdvMustHaveOnlyDesktopOrIndependentPositions,
                    };

            public IQueryable<Order.SelfAdvertisementPosition> GetSource()
                => (from order in _query.For<Facts::Order>()
                    from orderPosition in _query.For<Facts::OrderPosition>().Where(x => x.OrderId == order.Id)
                    from orderPositionAdvertisement in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == orderPosition.Id)
                    from position in _query.For<Facts::Position>()
                        .Where(x => !x.IsDeleted && x.CategoryCode == SelfAdvertisementOnlyOnPc)
                        .Where(x => x.Id == orderPositionAdvertisement.PositionId)
                    select new Order.SelfAdvertisementPosition { OrderId = orderPosition.OrderId }).Distinct();

            public FindSpecification<Order.SelfAdvertisementPosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<ReplaceValueObjectCommand>().Select(c => c.AggregateRootId).Distinct().ToArray();
                return new FindSpecification<Order.SelfAdvertisementPosition>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class OrderFirmOrganiationUnitMismatchAccessor : DataChangesHandler<Order.FirmOrganiationUnitMismatch>, IStorageBasedDataObjectAccessor<Order.FirmOrganiationUnitMismatch>
        {
            private readonly IQuery _query;

            public OrderFirmOrganiationUnitMismatchAccessor(IQuery query) :base(CreateInvalidator())
            {
                _query = query;
            }

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        MessageTypeCode.FirmAndOrderShouldBelongTheSameOrganizationUnit,
                    };

            public IQueryable<Order.FirmOrganiationUnitMismatch> GetSource()
                => from order in _query.For<Facts::Order>()
                   from firm in _query.For<Facts::Firm>().Where(x => x.IsActive && !x.IsDeleted && !x.IsClosedForAscertainment).Where(x => x.Id == order.FirmId)
                   where order.DestOrganizationUnitId != firm.OrganizationUnitId
                   select new Order.FirmOrganiationUnitMismatch { OrderId = order.Id };

            public FindSpecification<Order.FirmOrganiationUnitMismatch> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<ReplaceValueObjectCommand>().Select(c => c.AggregateRootId).Distinct().ToArray();
                return new FindSpecification<Order.FirmOrganiationUnitMismatch>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class OrderInvalidFirmAccessor : DataChangesHandler<Order.InvalidFirm>, IStorageBasedDataObjectAccessor<Order.InvalidFirm>
        {
            private readonly IQuery _query;

            public OrderInvalidFirmAccessor(IQuery query) : base(CreateInvalidator())
            {
                _query = query;
            }

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        MessageTypeCode.LinkedFirmShouldBeValid,
                    };

            public IQueryable<Order.InvalidFirm> GetSource()
                => from order in _query.For<Facts::Order>()
                   from firm in _query.For<Facts::Firm>().Where(x => !x.IsActive || x.IsDeleted || x.IsClosedForAscertainment).Where(x => x.Id == order.FirmId)
                   let state = firm.IsDeleted ? InvalidFirmState.Deleted
                                   : !firm.IsActive ? InvalidFirmState.ClosedForever
                                   : firm.IsClosedForAscertainment ? InvalidFirmState.ClosedForAscertainment
                                   : InvalidFirmState.NotSet
                   select new Order.InvalidFirm
                   {
                       FirmId = firm.Id,
                       OrderId = order.Id,
                       State = state,
                   };

            public FindSpecification<Order.InvalidFirm> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().Select(c => c.AggregateRootId).Distinct().ToArray();
                return new FindSpecification<Order.InvalidFirm>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class OrderCategoryPurchaseAccessor : DataChangesHandler<Order.CategoryPurchase>, IStorageBasedDataObjectAccessor<Order.CategoryPurchase>
        {
            private readonly IQuery _query;

            public OrderCategoryPurchaseAccessor(IQuery query) : base(CreateInvalidator())
            {
                _query = query;
            }

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        MessageTypeCode.FirmShouldHaveLimitedCategoryCount,
                    };

            public IQueryable<Order.CategoryPurchase> GetSource()
                => (from order in _query.For<Facts::Order>()
                   from orderPosition in _query.For<Facts::OrderPosition>().Where(x => x.OrderId == order.Id)
                   from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == orderPosition.Id).Where(x => x.CategoryId.HasValue)
                   select new Order.CategoryPurchase { OrderId = order.Id, CategoryId = opa.CategoryId.Value })
                   .Distinct();

            public FindSpecification<Order.CategoryPurchase> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<ReplaceValueObjectCommand>().Select(c => c.AggregateRootId).Distinct().ToArray();
                return new FindSpecification<Order.CategoryPurchase>(x => aggregateIds.Contains(x.OrderId));
            }
        }
    }
}
