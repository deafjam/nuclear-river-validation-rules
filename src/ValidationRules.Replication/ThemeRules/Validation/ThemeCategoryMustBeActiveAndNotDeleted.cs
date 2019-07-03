using System.Linq;

using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.ThemeRules;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.ThemeRules.Validation
{
    /// <summary>
    /// Для тематик, которые имеют продажи и привязаны к неактивным рубрикам, должна выводиться ошибка
    /// "Тематика {0} использует удаленную рубрику {1}"
    /// 
    /// Source: ThemeCategoriesValidationRule/ThemeUsesInactiveCategory
    /// </summary>
    public sealed class ThemeCategoryMustBeActiveAndNotDeleted : ValidationResultAccessorBase
    {
        public ThemeCategoryMustBeActiveAndNotDeleted(IQuery query) : base(query, MessageTypeCode.ThemeCategoryMustBeActiveAndNotDeleted)
        {
        }

        protected override IQueryable<Version.ValidationResult> GetValidationResults(IQuery query)
        {
            var invalidPeriods =
                from order in query.For<Order>()
                from orderTheme in query.For<Order.OrderTheme>().Where(x => x.OrderId == order.Id)
                from invalidCategory in query.For<Theme.InvalidCategory>().Where(x => x.ThemeId == orderTheme.ThemeId)
                select new
                    {
                        orderTheme.ThemeId,
                        invalidCategory.CategoryId,
                        order.Start,
                        order.End,
                        order.ProjectId
                    };

            var invalidMaxPeriods =
                from invalidPeriod in invalidPeriods
                group invalidPeriod by new { invalidPeriod.ProjectId, invalidPeriod.ThemeId, invalidPeriod.CategoryId }
                into grps
                select new
                    {
                        grps.Key.ThemeId,
                        grps.Key.CategoryId,
                        Start = grps.Min(x => x.Start),
                        End = grps.Max(x => x.End),
                        grps.Key.ProjectId,
                    };

            var ruleResults =
                from invalidMaxPeriod in invalidMaxPeriods
                select new Version.ValidationResult
                    {
                        MessageParams =
                            new MessageParams(
                                    new Reference<EntityTypeTheme>(invalidMaxPeriod.ThemeId),
                                    new Reference<EntityTypeCategory>(invalidMaxPeriod.CategoryId))
                                .ToXDocument(),

                        PeriodStart = invalidMaxPeriod.Start,
                        PeriodEnd = invalidMaxPeriod.End,
                        ProjectId = invalidMaxPeriod.ProjectId,
                    };

            return ruleResults;
        }
    }
}
