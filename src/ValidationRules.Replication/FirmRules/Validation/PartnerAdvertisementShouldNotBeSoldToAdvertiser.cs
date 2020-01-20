using System.Linq;

using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules;
using NuClear.ValidationRules.Storage.Model.Messages;

using Version = NuClear.ValidationRules.Storage.Model.Messages.Version;

namespace NuClear.ValidationRules.Replication.FirmRules.Validation
{
    /// <summary>
    /// Тесно связана с проверкой 75
    /// Для заказов, размещающих позиции партнёрской рекламы в карточках фирм-рекламодателей (исключая премиум), должно выводиться предупреждение.
    /// "Адрес {0} принадлежит фирме-рекламодателю {1} с заказом {2}"
    /// * Не выводить это сообщение в заказе, который размещает ЗМК в карточке своей-же фирмы.
    ///  Цель: Предупредить МПП, что ЗМК-карточка тоже рекламодатель, и почти наверняка его попросят убрать этот адрес  
    /// </summary>
    public sealed class PartnerAdvertisementShouldNotBeSoldToAdvertiser : ValidationResultAccessorBase
    {
        public PartnerAdvertisementShouldNotBeSoldToAdvertiser(IQuery query) : base(query, MessageTypeCode.PartnerAdvertisementShouldNotBeSoldToAdvertiser)
        {
        }

        protected override IQueryable<Version.ValidationResult> GetValidationResults(IQuery query)
        {
            var messages =
                from order in query.For<Order>()
                from fa in query.For<Order.PartnerPosition>().Where(x => x.OrderId == order.Id)
                from premium in query.For<Order.PremiumPartnerPosition>().Where(x => x.OrderId == order.Id).DefaultIfEmpty()
                from anotherOrder in query.For<Order>()
                    .Where(x => x.FirmId != order.FirmId && x.FirmId == fa.DestinationFirmId)
                    .Where(x => Scope.CanSee(order.Scope, x.Scope))
                    .Where(x => x.Start < order.End && order.Start < x.End)
                where premium == null
                select new Version.ValidationResult
                {
                    MessageParams =
                        new MessageParams(
                                new Reference<EntityTypeOrder>(order.Id), // Заказ, размещающий ссылку
                                new Reference<EntityTypeOrder>(anotherOrder.Id), // Заказ фирмы-рекламодателя (хоста)
                                new Reference<EntityTypeFirm>(fa.DestinationFirmId), // Фирма-рекламодатель (хост)
                                new Reference<EntityTypeFirmAddress>(fa.DestinationFirmAddressId))
                            .ToXDocument(),

                    PeriodStart = order.Start,
                    PeriodEnd = order.End,
                    OrderId = order.Id,
                };

            return messages;
        }
    }
}
