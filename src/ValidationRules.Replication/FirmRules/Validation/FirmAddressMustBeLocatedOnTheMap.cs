using System.Linq;

using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.FirmRules.Validation
{
    /// <summary>
    /// Для заказов, с привязками к не отмеченным на карте адресам, должна выводиться ошибка.
    /// "Позиция {0} оформлена на пустой адрес {1}"
    /// 
    /// Source: IsAdvertisementLinkedWithLocatedOnTheMapAddressOrderValidationRule
    /// * Проверка очень похожа на LinkedFirmAddressShouldBeValid, но запускается в ручном массовом. А в целом очень хочется их объединить.
    /// </summary>
    public sealed class FirmAddressMustBeLocatedOnTheMap : ValidationResultAccessorBase
    {
        public FirmAddressMustBeLocatedOnTheMap(IQuery query) : base(query, MessageTypeCode.FirmAddressMustBeLocatedOnTheMap)
        {
        }

        protected override IQueryable<Version.ValidationResult> GetValidationResults(IQuery query)
        {
            var ruleResults =
                from order in query.For<Order>()
                from advertisement in query.For<Order.AddressAdvertisementNonOnTheMap>().Where(x => x.OrderId == order.Id)
                select new Version.ValidationResult
                    {
                        MessageParams =
                            new MessageParams(
                                    new Reference<EntityTypeFirmAddress>(advertisement.AddressId),
                                    new Reference<EntityTypeOrder>(order.Id),
                                    new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                        new Reference<EntityTypeOrderPosition>(advertisement.OrderPositionId),
                                        new Reference<EntityTypePosition>(advertisement.PositionId)))
                                .ToXDocument(),

                        PeriodStart = order.Start,
                        PeriodEnd = order.End,
                        OrderId = order.Id,
                    };

            return ruleResults;
        }
    }
}