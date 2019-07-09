using System;
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
    public sealed class ThemeAggregateRootActor : AggregateRootActor<Theme>
    {
        public ThemeAggregateRootActor(
            IQuery query,
            IEqualityComparerFactory equalityComparerFactory,
            IBulkRepository<Theme> bulkRepository,
            IBulkRepository<Theme.InvalidCategory> invalidCategoryBulkRepository)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new ThemeAccessor(query), bulkRepository,
               HasValueObject(new InvalidCategoryAccessor(query), invalidCategoryBulkRepository));
        }

        public sealed class ThemeAccessor : DataChangesHandler<Theme>, IStorageBasedDataObjectAccessor<Theme>
        {
            private readonly IQuery _query;

            public ThemeAccessor(IQuery query) : base(CreateInvalidator(x => GetRelatedOrders(query, x))) => _query = query;

            private static IRuleInvalidator CreateInvalidator(Func<IReadOnlyCollection<Theme>, IEnumerable<long>> func)
                => new RuleInvalidator
                    {
                        {MessageTypeCode.DefaultThemeMustHaveOnlySelfAds, func},
                        {MessageTypeCode.ThemeCategoryMustBeActiveAndNotDeleted, func},
                        {MessageTypeCode.ThemePeriodMustContainOrderPeriod, func},
                    };

            private static IEnumerable<long> GetRelatedOrders(IQuery query, IReadOnlyCollection<Theme> dataObjects)
            {
                var themeIds = dataObjects.Select(x => x.Id).ToHashSet();
                return query.For<Order.OrderTheme>().Where(x => themeIds.Contains(x.ThemeId)).Select(x => x.OrderId);
            }
            
            public IQueryable<Theme> GetSource()
                => from theme in _query.For<Facts::Theme>()
                   select new Theme
                   {
                       Id = theme.Id,
                       BeginDistribution = theme.BeginDistribution,
                       EndDistribution = theme.EndDistribution,
                       IsDefault = theme.IsDefault,
                   };

            public FindSpecification<Theme> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
                return new FindSpecification<Theme>(x => aggregateIds.Contains(x.Id));
            }
        }

        public sealed class InvalidCategoryAccessor : DataChangesHandler<Theme.InvalidCategory>, IStorageBasedDataObjectAccessor<Theme.InvalidCategory>
        {
            private readonly IQuery _query;

            public InvalidCategoryAccessor(IQuery query) : base(CreateInvalidator(x => GetRelatedOrders(query, x))) => _query = query;

            private static IRuleInvalidator CreateInvalidator(Func<IReadOnlyCollection<Theme.InvalidCategory>, IEnumerable<long>> func)
                => new RuleInvalidator
                    {
                        {MessageTypeCode.ThemeCategoryMustBeActiveAndNotDeleted, func},
                    };

            private static IEnumerable<long> GetRelatedOrders(IQuery query, IReadOnlyCollection<Theme.InvalidCategory> dataObjects)
            {
                var themeIds = dataObjects.Select(x => x.ThemeId).ToHashSet();
                return query.For<Order.OrderTheme>().Where(x => themeIds.Contains(x.ThemeId)).Select(x => x.OrderId);
            }
            
            public IQueryable<Theme.InvalidCategory> GetSource()
            {
                var invalidCategories =
                    from themeCategory in _query.For<Facts::ThemeCategory>()
                    from category in _query.For<Facts::Category>().Where(x => !x.IsActiveNotDeleted).Where(x => x.Id == themeCategory.CategoryId)
                    select new Theme.InvalidCategory
                    {
                        ThemeId = themeCategory.ThemeId,
                        CategoryId = themeCategory.CategoryId,
                    };

                return invalidCategories;
            }

            public FindSpecification<Theme.InvalidCategory> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Theme.InvalidCategory>(x => aggregateIds.Contains(x.ThemeId));
            }
        }
    }
}
