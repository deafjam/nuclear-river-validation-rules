using System.Collections.Generic;
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
    /// Для заказов, размещающих рекламу в карточке другой фирмы (исключая премиум), если для одного адреса есть более одной продажи, должно выводиться предупреждение.
    /// "На адрес {0} фирмы {1} продано более одной позиции 'Реклама в профилях партнёров' в периоды: {2}"
    /// </summary>
    public sealed class FirmAddressShouldNotHaveMultiplePartnerAdvertisement : ValidationResultAccessorBase
    {
        public FirmAddressShouldNotHaveMultiplePartnerAdvertisement(IQuery query) : base(query, MessageTypeCode.FirmAddressShouldNotHaveMultiplePartnerAdvertisement)
        {
        }

        protected override IQueryable<Version.ValidationResult> GetValidationResults(IQuery query)
        {
            var sales =
                from order in query.For<Order>()
                from fa in query.For<Order.PartnerPosition>().Where(x => x.OrderId == order.Id)
                from premium in query.For<Order.PremiumPartnerPosition>().Where(x => x.OrderId == order.Id).DefaultIfEmpty()
                select new { fa.OrderId, fa.OrderPositionId, FirmAddressId = fa.DestinationFirmAddressId, FirmId = fa.DestinationFirmId, IsPremium = premium != null, order.Scope, order.Start, order.End };

            var multipleSales =
                from sale in sales
                from conflict in sales.Where(x => x.FirmAddressId == sale.FirmAddressId && x.OrderPositionId != sale.OrderPositionId)
                where sale.Start < conflict.End && conflict.Start < sale.End && Scope.CanSee(sale.Scope, conflict.Scope)
                    && (!sale.IsPremium || !conflict.IsPremium) // Если обе премиум-позиции - то это уже ответственность другой проверки
                select new { sale.OrderId, sale.FirmAddressId, sale.FirmId, Start = sale.Start < conflict.Start ? conflict.Start : sale.Start, End = sale.End < conflict.End ? sale.End : conflict.End };

            multipleSales =
                multipleSales.GroupBy(x => new { x.OrderId, x.FirmAddressId, x.FirmId })
                             .Select(x => new { x.Key.OrderId, x.Key.FirmAddressId, x.Key.FirmId, Start = x.Min(y => y.Start), End = x.Max(y => y.End) });

            var messages =
                from sale in multipleSales
                select new Version.ValidationResult
                {
                    MessageParams =
                            new MessageParams(
                                    new Dictionary<string, object> { { "start", sale.Start }, { "end", sale.End } },
                                    new Reference<EntityTypeOrder>(sale.OrderId),
                                    new Reference<EntityTypeFirm>(sale.FirmId),
                                    new Reference<EntityTypeFirmAddress>(sale.FirmAddressId))
                                .ToXDocument(),

                    PeriodStart = sale.Start,
                    PeriodEnd = sale.End,
                    OrderId = sale.OrderId,
                };

            return messages;
        }
    }
}
