using System.Collections.Generic;
using System.Linq;

using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;


namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement PoiAmountForEntranceShouldMeetMaximumRestrictionsF2A
            => ArrangeMetadataElement
               .Config
               .Name(nameof(PoiAmountForEntranceShouldMeetMaximumRestrictionsF2A))
               .Fact(
                     new Facts::Order { Id = 1, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(2) },
                     new Facts::Order { Id = 2, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(2) },
                     new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, FirmAddressId = 1, PositionId = 1 },
                     new Facts::OrderPositionAdvertisement {OrderId = 2, OrderPositionId = 2, FirmAddressId = 2, PositionId = 2 },
                     new Facts::FirmAddress { Id = 1, EntranceCode = 1 },
                     new Facts::FirmAddress { Id = 2, EntranceCode = 2 },
                     new Facts::Position { Id = 1, CategoryCode = Facts::Position.CategoryCodesPoiAddressCheck.First() },
                     new Facts::Position { Id = 2 })
               .Aggregate(new Order.EntranceControlledPosition { OrderId = 1, OrderPositionId = 1, EntranceCode = 1, FirmAddressId = 1});


        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement PoiAmountForEntranceShouldMeetMaximumRestrictionsA2M
            => ArrangeMetadataElement
               .Config
               .Name(nameof(PoiAmountForEntranceShouldMeetMaximumRestrictionsA2M))
               .Aggregate(
                      new Order.OrderPeriod { OrderId = 1, Start = MonthStart(1), End = MonthStart(3), Scope = 0 },
                      new Order.EntranceControlledPosition { OrderId = 1, OrderPositionId = 1, EntranceCode = 1, FirmAddressId = 1 },
                      new Order.EntranceControlledPosition { OrderId = 1, OrderPositionId = ~1, EntranceCode = 1, FirmAddressId = 1 },

                      new Order.OrderPeriod { OrderId = 2, Start = MonthStart(1), End = MonthStart(3), Scope = 0 },
                      new Order.EntranceControlledPosition { OrderId = 2, OrderPositionId = 2, EntranceCode = 1, FirmAddressId = 1 },

                      new Order.OrderPeriod { OrderId = 3, Start = MonthStart(1), End = MonthStart(2), Scope = -1 },
                      new Order.EntranceControlledPosition { OrderId = 3, OrderPositionId = 3, EntranceCode = 1, FirmAddressId = 1 },

                      new Order.OrderPeriod { OrderId = 4, Start = MonthStart(1), End = MonthStart(2), Scope = 4 },
                      new Order.EntranceControlledPosition { OrderId = 4, OrderPositionId = 4, EntranceCode = 2, FirmAddressId = 2 },

                      new Order.OrderPeriod { OrderId = 5, Start = MonthStart(2), End = MonthStart(3), Scope = 5 },
                      new Order.EntranceControlledPosition { OrderId = 5, OrderPositionId = 5, EntranceCode = 1, FirmAddressId = 1 })
               .Message(
                       new Version.ValidationResult
                       {
                           MessageParams =
                               new MessageParams(
                                   new Dictionary<string, object> { { "start", MonthStart(1) }, { "end", MonthStart(3) }, { "maxCount", 1 }, { "entranceCode", 1 } },
                                   new Reference<EntityTypeOrder>(1),
                                   new Reference<EntityTypeFirmAddress>(1)).ToXDocument(),
                           MessageType = (int)MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions,
                           PeriodStart = MonthStart(1),
                           PeriodEnd = MonthStart(3),
                           OrderId = 1,
                       },
                        new Version.ValidationResult
                            {
                                MessageParams =
                                    new MessageParams(
                                                      new Dictionary<string, object> { { "start", MonthStart(1) }, { "end", MonthStart(3) }, { "maxCount", 1 }, { "entranceCode", 1 } },
                                                      new Reference<EntityTypeOrder>(2),
                                                      new Reference<EntityTypeFirmAddress>(1)).ToXDocument(),
                                MessageType = (int)MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions,
                                PeriodStart = MonthStart(1),
                                PeriodEnd = MonthStart(3),
                                OrderId = 1,
                            },
                        new Version.ValidationResult
                            {
                                MessageParams =
                                    new MessageParams(
                                                      new Dictionary<string, object> { { "start", MonthStart(1) }, { "end", MonthStart(3) }, { "maxCount", 1 }, { "entranceCode", 1 } },
                                                      new Reference<EntityTypeOrder>(1),
                                                      new Reference<EntityTypeFirmAddress>(1)).ToXDocument(),
                                MessageType = (int)MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions,
                                PeriodStart = MonthStart(1),
                                PeriodEnd = MonthStart(3),
                                OrderId = 2,
                            },
                        new Version.ValidationResult
                            {
                                MessageParams =
                                    new MessageParams(
                                                      new Dictionary<string, object> { { "start", MonthStart(1) }, { "end", MonthStart(2) }, { "maxCount", 1 }, { "entranceCode", 1 } },
                                                      new Reference<EntityTypeOrder>(1),
                                                      new Reference<EntityTypeFirmAddress>(1)).ToXDocument(),
                                MessageType = (int)MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions,
                                PeriodStart = MonthStart(1),
                                PeriodEnd = MonthStart(2),
                                OrderId = 3,
                            },
                        new Version.ValidationResult
                            {
                                MessageParams =
                                    new MessageParams(
                                                      new Dictionary<string, object> { { "start", MonthStart(1) }, { "end", MonthStart(2) }, { "maxCount", 1 }, { "entranceCode", 1 } },
                                                      new Reference<EntityTypeOrder>(2),
                                                      new Reference<EntityTypeFirmAddress>(1)).ToXDocument(),
                                MessageType = (int)MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions,
                                PeriodStart = MonthStart(1),
                                PeriodEnd = MonthStart(2),
                                OrderId = 3,
                            },
                        new Version.ValidationResult
                            {
                                MessageParams =
                                    new MessageParams(
                                                      new Dictionary<string, object> { { "start", MonthStart(2) }, { "end", MonthStart(3) }, { "maxCount", 1 }, { "entranceCode", 1 } },
                                                      new Reference<EntityTypeOrder>(1),
                                                      new Reference<EntityTypeFirmAddress>(1)).ToXDocument(),
                                MessageType = (int)MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions,
                                PeriodStart = MonthStart(2),
                                PeriodEnd = MonthStart(3),
                                OrderId = 5,
                            },
                        new Version.ValidationResult
                            {
                                MessageParams =
                                    new MessageParams(
                                                      new Dictionary<string, object> { { "start", MonthStart(2) }, { "end", MonthStart(3) }, { "maxCount", 1 }, { "entranceCode", 1 } },
                                                      new Reference<EntityTypeOrder>(2),
                                                      new Reference<EntityTypeFirmAddress>(1)).ToXDocument(),
                                MessageType = (int)MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions,
                                PeriodStart = MonthStart(2),
                                PeriodEnd = MonthStart(3),
                                OrderId = 5,
                            });
    }
}