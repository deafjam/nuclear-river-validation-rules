using System.Linq;

using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.FirmRules.Validation
{
    /// <summary>
    /// Для заказов, фирма которых находится не в городе назначения заказа, должна выводиться ошибка.
    /// "Отделение организации назначения заказа не соответствует отделению организации выбранной фирмы"
    /// 
    /// Source: FirmBelongsToOrdersDestOrgUnitOrderValidationRule
    /// </summary>
    public sealed class FirmAndOrderShouldBelongTheSameOrganizationUnit : ValidationResultAccessorBase
    {
        public FirmAndOrderShouldBelongTheSameOrganizationUnit(IQuery query) : base(query, MessageTypeCode.FirmAndOrderShouldBelongTheSameOrganizationUnit)
        {
        }

        protected override IQueryable<Version.ValidationResult> GetValidationResults(IQuery query)
        {
            var messages =
                from fail in query.For<Order.FirmOrganizationUnitMismatch>()
                from order in query.For<Order>().Where(x => x.Id == fail.OrderId)
                select new Version.ValidationResult
                    {
                        MessageParams =
                            new MessageParams(
                                    new Reference<EntityTypeFirm>(order.FirmId),
                                    new Reference<EntityTypeOrder>(order.Id))
                                .ToXDocument(),

                        PeriodStart = order.Start,
                        PeriodEnd = order.End,
                        OrderId = order.Id,
                    };

            return messages;
        }
    }
}
