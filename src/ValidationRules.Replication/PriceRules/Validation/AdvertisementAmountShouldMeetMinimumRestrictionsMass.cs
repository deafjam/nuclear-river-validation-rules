using System.Collections.Generic;
using System.Linq;

using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.PriceRules.Validation
{
    /// <summary>
    /// Для проекта, в котором продано недостаточно рекламы в Position.Category должна выводиться ошибка.
    /// "Позиция {0} должна присутствовать в сборке в количестве от {1} до {2}. Фактическое количество позиций в месяц {6} - {3} (оформлено - {4}, содержит ошибки - {5})"
    /// В силу того, что мы (пока) не знаем, число заказов с ошибками, сократим сообщение (erm тоже так делает иногда)
    /// "Позиция {0} должна присутствовать в сборке в количестве от {1} до {2}. Фактическое количество позиций в месяц {3} - {4}"
    /// 
    /// Source: AdvertisementAmountOrderValidationRule/AdvertisementAmountErrorMessage
    /// </summary>
    public sealed class AdvertisementAmountShouldMeetMinimumRestrictionsMass : ValidationResultAccessorBase
    {
        public AdvertisementAmountShouldMeetMinimumRestrictionsMass(IQuery query) : base(query, MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictionsMass)
        {
        }

        protected override IQueryable<Version.ValidationResult> GetValidationResults(IQuery query)
        {
            var restrictionGrid =
                from period in query.For<Period>()
                from restriction in query.For<Ruleset.AdvertisementAmountRestriction>().Where(x => x.Begin < period.End && period.Start < x.End)
                select new { period.Start, period.End, restriction.ProjectId, restriction.CategoryCode, restriction.Min, restriction.Max };

            var saleGrid =
                from orderPeriod in query.For<Order.OrderPeriod>()
                from position in query.For<Order.AmountControlledPosition>().Where(x => orderPeriod.OrderId == x.OrderId)
                select new { orderPeriod.Start, orderPeriod.End, orderPeriod.Scope, position.OrderId, position.ProjectId, position.CategoryCode };

            var violations =
                from restriction in restrictionGrid
                let count = saleGrid.Count(x => x.CategoryCode == restriction.CategoryCode &&
                                                x.ProjectId == restriction.ProjectId &&
                                                x.Start <= restriction.Start && restriction.End <= x.End &&
                                                x.Scope == 0)
                select new { restriction.ProjectId, restriction.Start, restriction.End, restriction.CategoryCode, restriction.Min, restriction.Max, Count = count };

            var messages =
                from violation in violations
                where !(violation.Min <= violation.Count && violation.Count <= violation.Max)
                select new Version.ValidationResult
                    {
                        MessageParams =
                            new MessageParams(
                                    new Dictionary<string, object>
                                        {
                                            { "min", violation.Min },
                                            { "max", violation.Max },
                                            { "count", violation.Count },
                                            { "start", violation.Start },
                                            { "end", violation.End },
                                        },
                                    new Reference<EntityTypeProject>(violation.ProjectId),
                                    new Reference<EntityTypeNomenclatureCategory>(violation.CategoryCode))
                                .ToXDocument(),

                        PeriodStart = violation.Start,
                        PeriodEnd = violation.End,
                        ProjectId = violation.ProjectId,
                    };

            return messages;
        }
    }
}
