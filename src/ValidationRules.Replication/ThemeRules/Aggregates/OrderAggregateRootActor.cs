using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Storage.Model.Aggregates.ThemeRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.ThemeRules.Aggregates
{
    public sealed class OrderAggregateRootActor : AggregateRootActor<Order>
    {
        public OrderAggregateRootActor(
            IQuery query,
            IEqualityComparerFactory equalityComparerFactory,
            IBulkRepository<Order> bulkRepository,
            IBulkRepository<Order.OrderTheme> orderThemeBulkRepository)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new OrderAccessor(query), bulkRepository,
               HasValueObject(new OrderThemeAccessor(query), orderThemeBulkRepository));
        }

        public sealed class OrderAccessor : DataChangesHandler<Order>, IStorageBasedDataObjectAccessor<Order>
        {
            private readonly IQuery _query;

            public OrderAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.DefaultThemeMustHaveOnlySelfAds, GetRelatedOrders},
                        {MessageTypeCode.ThemeCategoryMustBeActiveAndNotDeleted, GetRelatedOrders},
                        {MessageTypeCode.ThemePeriodMustContainOrderPeriod, GetRelatedOrders},
                    };
            
            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order> dataObjects) =>
                dataObjects.Select(x => x.Id);

            public IQueryable<Order> GetSource()
                =>
                    _query.For<Facts::Order>()
                        .Select(order => new Order
                        {
                            Id = order.Id,
                            Start = order.AgileDistributionStartDate,
                            End = order.AgileDistributionEndFactDate,
                            ProjectId = order.ProjectId,
                            IsSelfAds = order.IsSelfAds,
                        });

            public FindSpecification<Order> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
                return new FindSpecification<Order>(x => aggregateIds.Contains(x.Id));
            }
        }

        public sealed class OrderThemeAccessor : DataChangesHandler<Order.OrderTheme>, IStorageBasedDataObjectAccessor<Order.OrderTheme>
        {
            private readonly IQuery _query;

            public OrderThemeAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        {MessageTypeCode.DefaultThemeMustHaveOnlySelfAds, GetRelatedOrders},
                        {MessageTypeCode.ThemeCategoryMustBeActiveAndNotDeleted, GetRelatedOrders},
                        {MessageTypeCode.ThemePeriodMustContainOrderPeriod, GetRelatedOrders},
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Order.OrderTheme> dataObjects) =>
                dataObjects.Select(x => x.OrderId);
            
            public IQueryable<Order.OrderTheme> GetSource()
            {
                var orderThemes =
                    _query.For<Facts::OrderPositionAdvertisement>()
                    .Where(x => x.ThemeId != null)
                    .Select(x => new Order.OrderTheme
                    {
                        OrderId = x.OrderId,
                        ThemeId = x.ThemeId.Value
                    })
                    .Distinct();

                return orderThemes;
            }

            public FindSpecification<Order.OrderTheme> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Order.OrderTheme>(x => aggregateIds.Contains(x.OrderId));
            }
        }
    }
}
