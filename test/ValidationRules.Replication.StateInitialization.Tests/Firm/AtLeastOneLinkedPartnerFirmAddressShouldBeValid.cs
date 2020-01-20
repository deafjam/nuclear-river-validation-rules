using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using Messages = NuClear.ValidationRules.Storage.Model.Messages;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        private static ArrangeMetadataElement AtLeastOneLinkedPartnerFirmAddressShouldBeValid
            => ArrangeMetadataElement
                .Config
                .Name(nameof(AtLeastOneLinkedPartnerFirmAddressShouldBeValid))
                .Fact(
                    // buy here with only invalid addresses
                    new Facts::Order { Id = 1, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(2) },
                    new Facts::OrderWorkflow { Id = 1, Step = 5 },
                    new Facts::OrderPosition { Id = 1, OrderId = 1, PricePositionId = 1},
                    new Facts::PricePosition { Id = 1, PositionId = 2 },

                    new Facts::OrderPositionAdvertisement { OrderPositionId = 1, FirmAddressId = 1, PositionId = 1 },
                    new Facts::FirmAddressInactive { Id = 1 },

                    // buy here with invalid and valid addresses
                    new Facts::Order { Id = 2, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(2) },
                    new Facts::OrderWorkflow { Id = 2, Step = 5 },
                    new Facts::OrderPosition { Id = 2, OrderId = 2, PricePositionId = 1 },
                    new Facts::OrderPositionAdvertisement { OrderPositionId = 2, FirmAddressId = 2, PositionId = 1 },
                    new Facts::FirmAddressInactive { Id = 2 },
                    new Facts::OrderPositionAdvertisement { OrderPositionId = 2, FirmAddressId = 3, PositionId = 1 },
                    new Facts::FirmAddress { Id = 3 },

                    // non buy here with only invalid addresses
                    new Facts::Order { Id = 3, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(2) },
                    new Facts::OrderWorkflow { Id = 3, Step = 5 },
                    new Facts::OrderPosition { Id = 3, OrderId = 3, PricePositionId = 1 },
                    new Facts::OrderPositionAdvertisement { OrderPositionId = 3, FirmAddressId = 4, PositionId = 3 },
                    new Facts::FirmAddressInactive { Id = 4 },

                    new Facts::Position { Id = 1, CategoryCode = Facts::Position.CategoryCodePartnerAdvertisingAddress },
                    new Facts::Position { Id = 2 },
                    new Facts::Position { Id = 3 })
                .Aggregate(
                    new Order { Id = 1, Start = MonthStart(1), End = MonthStart(2) },
                    new Order { Id = 2, Start = MonthStart(1), End = MonthStart(2) },
                    new Order { Id = 3, Start = MonthStart(1), End = MonthStart(2) },
                    new Order.MissingValidPartnerFirmAddresses { OrderId = 1, OrderPositionId = 1, PositionId = 2 })
                .Message(
                    new Messages::Version.ValidationResult
                        {
                            MessageParams = new MessageParams(
                                    new Reference<EntityTypeOrderPosition>(1,
                                                                           new Reference<EntityTypeOrder>(1),
                                                                           new Reference<EntityTypePosition>(2)))
                                .ToXDocument(),
                            MessageType = (int)MessageTypeCode.AtLeastOneLinkedPartnerFirmAddressShouldBeValid,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 1
                        });
    }
}