using System.Linq;

using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.FirmRules.Validation
{
    public sealed class AtLeastOneLinkedPartnerFirmAddressShouldBeValid : ValidationResultAccessorBase
    {
        public AtLeastOneLinkedPartnerFirmAddressShouldBeValid(IQuery query) : base(query, MessageTypeCode.AtLeastOneLinkedPartnerFirmAddressShouldBeValid)
        {
        }

        protected override IQueryable<Version.ValidationResult> GetValidationResults(IQuery query)
        {
            var ruleResults =
                from order in query.For<Order>()
                from missingAddress in query.For<Order.MissingValidPartnerFirmAddresses>().Where(x => x.OrderId == order.Id)
                select new Version.ValidationResult
                    {
                        MessageParams =
                            new MessageParams(new Reference<EntityTypeOrderPosition>(missingAddress.OrderPositionId,
                                                                                     new Reference<EntityTypeOrder>(order.Id),
                                                                                     new Reference<EntityTypePosition>(missingAddress.PositionId)))
                                .ToXDocument(),

                        PeriodStart = order.Start,
                        PeriodEnd = order.End,
                        OrderId = order.Id,
                    };

            return ruleResults;
        }
    }
}