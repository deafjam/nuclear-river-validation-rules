using System.Linq;

using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.ThemeRules;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.ThemeRules.Validation
{
    /// <summary>
    /// Для заказов, период размещения которых не вложен в период действия тематики, должна выводиться ошибка
    /// "Заказ {0} не может иметь продаж в тематику {1}, поскольку тематика действует не весь период размещения заказа"
    /// 
    /// Source: ThemePeriodOverlapsOrderPeriodValidationRule/ThemePeriodDoesNotOverlapOrderPeriod
    /// </summary>
    public sealed class ThemePeriodMustContainOrderPeriod : ValidationResultAccessorBase
    {
        public ThemePeriodMustContainOrderPeriod(IQuery query) : base(query, MessageTypeCode.ThemePeriodMustContainOrderPeriod)
        {
        }

        protected override IQueryable<Version.ValidationResult> GetValidationResults(IQuery query)
        {
            var ruleResults =
                from order in query.For<Order>()
                from orderTheme in query.For<Order.OrderTheme>().Where(x => x.OrderId == order.Id)
                from theme in query.For<Theme>().Where(x => x.Id == orderTheme.ThemeId)
                where theme.BeginDistribution > order.Start || // тематика начинает размещаться позже заказа
                      order.End > theme.EndDistribution // тематика оканчивает размещаться раньше заказа
                select new Version.ValidationResult
                    {
                        MessageParams =
                            new MessageParams(
                                    new Reference<EntityTypeOrder>(order.Id),
                                    new Reference<EntityTypeTheme>(orderTheme.ThemeId))
                                .ToXDocument(),

                        PeriodStart = order.Start,
                        PeriodEnd = order.End,
                        OrderId = order.Id,
                    };

            return ruleResults;
        }
    }
}
