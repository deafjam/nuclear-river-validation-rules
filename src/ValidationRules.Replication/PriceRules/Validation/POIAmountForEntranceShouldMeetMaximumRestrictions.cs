using System.Collections.Generic;
using System.Linq;

using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.PriceRules.Validation
{
    /// <summary>
    /// При превышении допустимого количества POI на один вход должна выводиться ошибка:
    /// "Превышено допустимое количество POI на вход: {0}. Месяц: {1}. Адрес: {2}. Вход: {3}. Конфликтующие заказы: {4}"
    /// </summary>
    public class PoiAmountForEntranceShouldMeetMaximumRestrictions : ValidationResultAccessorBase
    {
        private const int MaxSalesOnEntrance = 1;

        public PoiAmountForEntranceShouldMeetMaximumRestrictions(IQuery query) : base(query, MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions)
        {
        }

        protected override IQueryable<Version.ValidationResult> GetValidationResults(IQuery query)
        {
            var periods = 
                from op in query.For<Order.OrderPeriod>()
                from fa in query.For<Order.EntranceControlledPosition>().Where(x => x.OrderId == op.OrderId)
                select new
                {
                    op.OrderId,
                    op.ProjectId,
                    op.Start,
                    op.End,
                    op.Scope,
                    fa.OrderPositionId,
                    fa.EntranceCode,
                    fa.FirmAddressId
                };

            var result =
                from period in periods
                from conflictPeriod in periods
                    .Where(x =>
                        x.OrderPositionId != period.OrderPositionId &&
                        x.EntranceCode == period.EntranceCode &&
                        x.ProjectId == period.ProjectId &&
                        x.Start <= period.Start && period.End <= x.End &&
                        Scope.CanSee(period.Scope, x.Scope))
                select new Version.ValidationResult
                    {
                        MessageParams =
                            new MessageParams(new Dictionary<string, object>
                                                  {
                                                      { "start", period.Start },
                                                      { "end", period.End },
                                                      { "maxCount", MaxSalesOnEntrance },
                                                      { "entranceCode", period.EntranceCode }
                                                  },
                                              new Reference<EntityTypeOrder>(conflictPeriod.OrderId),
                                              new Reference<EntityTypeFirmAddress>(period.FirmAddressId))
                                .ToXDocument(),
                    
                        PeriodStart = period.Start,
                        PeriodEnd = period.End,
                        OrderId = period.OrderId
                    };

            return result;
        }
    }
}