using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Storage.Model.Aggregates.AdvertisementRules;
using NuClear.ValidationRules.Storage.Model.Messages;

using Facts = NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.AdvertisementRules.Aggregates
{
    public sealed class OrderAggregateRootActor : AggregateRootActor<Order>
    {

        public OrderAggregateRootActor(
            IQuery query,
            IEqualityComparerFactory equalityComparerFactory,
            IBulkRepository<Order> repository,
            IBulkRepository<Order.MissingAdvertisementReference> missingAdvertisementReferenceRepository,
            IBulkRepository<Order.MissingOrderPositionAdvertisement> missingOrderPositionAdvertisementRepository,
            IBulkRepository<Order.AdvertisementNotBelongToFirm> advertisementNotBelongToFirmRepository,
            IBulkRepository<Order.AdvertisementFailedReview> advertisementFailedReviewRepository)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new OrderAccessor(query), repository,
                HasValueObject(new MissingAdvertisementReferenceAccessor(query), missingAdvertisementReferenceRepository),
                HasValueObject(new MissingOrderPositionAdvertisementAccessor(query), missingOrderPositionAdvertisementRepository),
                HasValueObject(new AdvertisementNotBelongToFirmAccessor(query), advertisementNotBelongToFirmRepository),
                HasValueObject(new AdvertisementFailedReviewAccessor(query), advertisementFailedReviewRepository));
        }

        public sealed class OrderAccessor : DataChangesHandler<Order>, IStorageBasedDataObjectAccessor<Order>
        {
            private readonly IQuery _query;

            public OrderAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.OrderPositionAdvertisementMustBeCreated, GetRelatedOrders},
                        {MessageTypeCode.OrderPositionAdvertisementMustHaveAdvertisement, GetRelatedOrders},
                        {MessageTypeCode.OrderPositionAdvertisementMustHaveOptionalAdvertisement, GetRelatedOrders},
                        {MessageTypeCode.AdvertisementMustPassReview, GetRelatedOrders},
                        {MessageTypeCode.OptionalAdvertisementMustPassReview, GetRelatedOrders},
                        {MessageTypeCode.AdvertisementShouldNotHaveComments, GetRelatedOrders},
                        {MessageTypeCode.AdvertisementMustBelongToFirm, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order> dataObjects) =>
                dataObjects.Select(x => x.Id);
            
            public IQueryable<Order> GetSource()
                => _query.For<Facts::Order>()
                    .Select(x => new Order
                    {
                        Id = x.Id,
                        Start = x.AgileDistributionStartDate,
                        End = x.AgileDistributionEndPlanDate,
                    });

            public FindSpecification<Order> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
                return new FindSpecification<Order>(x => aggregateIds.Contains(x.Id));
            }
        }

        public sealed class MissingAdvertisementReferenceAccessor : DataChangesHandler<Order.MissingAdvertisementReference>, IStorageBasedDataObjectAccessor<Order.MissingAdvertisementReference>
        {
            private readonly IQuery _query;

            public MissingAdvertisementReferenceAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.OrderPositionAdvertisementMustHaveAdvertisement, GetRelatedOrders},
                        {MessageTypeCode.OrderPositionAdvertisementMustHaveOptionalAdvertisement, GetRelatedOrders}
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.MissingAdvertisementReference> dataObjects) =>
                dataObjects.Select(x => x.OrderId);

            public IQueryable<Order.MissingAdvertisementReference> GetSource()
            {
                var positionChilds = from position in _query.For<Facts::Position>().Where(x => !x.IsDeleted)
                                     from child in _query.For<Facts::PositionChild>().Where(x => x.MasterPositionId == position.Id).DefaultIfEmpty()
                                     select new
                                         {
                                             PositionId = position.Id,
                                             ChildPositionId = child != null ? child.ChildPositionId : position.Id
                                         };

                var result =
                    from op in _query.For<Facts::OrderPosition>()
                    from pp in _query.For<Facts::PricePosition>().Where(x => x.IsActiveNotDeleted).Where(x => x.Id == op.PricePositionId)
                    from positionChild in positionChilds.Where(x => x.PositionId == pp.PositionId)
                    from p in _query.For<Facts::Position>().Where(x => x.ContentSales != Facts::Position.ContentSalesWithoutContent).Where(x => x.Id == positionChild.ChildPositionId)
                    from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == op.Id && x.PositionId == p.Id)
                    where opa.AdvertisementId == null // позиция IsContentSales и не указан advertisementId
                    select new Order.MissingAdvertisementReference
                        {
                            OrderId = op.OrderId,
                            OrderPositionId = op.Id,
                            CompositePositionId = pp.PositionId,
                            PositionId = p.Id,
                            AdvertisementIsOptional = p.ContentSales == Facts::Position.ContentSalesContentIsNotRequired
                        };

                return result.Distinct();
            }

            public FindSpecification<Order.MissingAdvertisementReference> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.MissingAdvertisementReference>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class MissingOrderPositionAdvertisementAccessor : DataChangesHandler<Order.MissingOrderPositionAdvertisement>, IStorageBasedDataObjectAccessor<Order.MissingOrderPositionAdvertisement>
        {
            private readonly IQuery _query;

            public MissingOrderPositionAdvertisementAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.OrderPositionAdvertisementMustBeCreated, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.MissingOrderPositionAdvertisement> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.MissingOrderPositionAdvertisement> GetSource()
            {
                var positionChilds = from position in _query.For<Facts::Position>().Where(x => !x.IsDeleted).Where(x => !x.IsCompositionOptional)
                                     from child in _query.For<Facts::PositionChild>().Where(x => x.MasterPositionId == position.Id).DefaultIfEmpty()
                                     select new
                                         {
                                             PositionId = position.Id,
                                             ChildPositionId = child != null ? child.ChildPositionId : position.Id
                                         };

                var result =
                       from op in _query.For<Facts::OrderPosition>()
                       from pp in _query.For<Facts::PricePosition>().Where(x => x.IsActiveNotDeleted).Where(x => x.Id == op.PricePositionId)
                       from positionChild in positionChilds.Where(x => x.PositionId == pp.PositionId)
                       from p in _query.For<Facts::Position>().Where(x => x.Id == positionChild.ChildPositionId)
                       from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == op.Id && x.PositionId == p.Id)
                                         .DefaultIfEmpty()
                       where opa == null // позиция не IsCompositionOptional и нет ни одной продажи
                       select new Order.MissingOrderPositionAdvertisement
                           {
                               OrderId = op.OrderId,
                               OrderPositionId = op.Id,
                               CompositePositionId = pp.PositionId,
                               PositionId = p.Id,
                           };

                return result;
            }

            public FindSpecification<Order.MissingOrderPositionAdvertisement> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.MissingOrderPositionAdvertisement>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class AdvertisementNotBelongToFirmAccessor : DataChangesHandler<Order.AdvertisementNotBelongToFirm>, IStorageBasedDataObjectAccessor<Order.AdvertisementNotBelongToFirm>
        {
            private readonly IQuery _query;

            public AdvertisementNotBelongToFirmAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.AdvertisementMustBelongToFirm, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.AdvertisementNotBelongToFirm> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.AdvertisementNotBelongToFirm> GetSource()
            {
                var result =
                    from order in _query.For<Facts::Order>()
                    from op in _query.For<Facts::OrderPosition>().Where(x => x.OrderId == order.Id)
                    from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == op.Id)
                    from advertisement in _query.For<Facts::Advertisement>().Where(x => x.Id == opa.AdvertisementId.Value && x.FirmId != order.FirmId)
                    select new Order.AdvertisementNotBelongToFirm
                        {
                            OrderId = order.Id,
                            AdvertisementId = opa.AdvertisementId.Value,
                            OrderPositionId = op.Id,
                            PositionId = opa.PositionId,
                            ExpectedFirmId = order.FirmId,
                            ActualFirmId = advertisement.FirmId,
                        };

                return result;
            }

            public FindSpecification<Order.AdvertisementNotBelongToFirm> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.AdvertisementNotBelongToFirm>(x => aggregateIds.Contains(x.OrderId));
            }
        }

        public sealed class AdvertisementFailedReviewAccessor : DataChangesHandler<Order.AdvertisementFailedReview>, IStorageBasedDataObjectAccessor<Order.AdvertisementFailedReview>
        {
            private readonly IQuery _query;

            public AdvertisementFailedReviewAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.AdvertisementMustPassReview, GetRelatedOrders},
                        {MessageTypeCode.AdvertisementShouldNotHaveComments, GetRelatedOrders},
                        {MessageTypeCode.OptionalAdvertisementMustPassReview, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.AdvertisementFailedReview> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.AdvertisementFailedReview> GetSource()
            {
                var result =
                    from order in _query.For<Facts::Order>().Where(x => !x.IsSelfSale)
                    from op in _query.For<Facts::OrderPosition>().Where(x => x.OrderId == order.Id)
                    from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == op.Id)
                    from p in _query.For<Facts::Position>().Where(x => x.Id == opa.PositionId)
                    from a in _query.For<Facts::Advertisement>().Where(x => x.StateCode != Facts::Advertisement.Ok).Where(x => x.Id == opa.AdvertisementId.Value)
                    select new Order.AdvertisementFailedReview
                        {
                            OrderId = order.Id,
                            AdvertisementId = opa.AdvertisementId.Value,
                            ReviewState = a.StateCode,

                            AdvertisementIsOptional = p.ContentSales == Facts::Position.ContentSalesContentIsNotRequired
                        };

                return result.Distinct();
            }

            public FindSpecification<Order.AdvertisementFailedReview> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.AdvertisementFailedReview>(x => aggregateIds.Contains(x.OrderId));
            }
        }
    }
}