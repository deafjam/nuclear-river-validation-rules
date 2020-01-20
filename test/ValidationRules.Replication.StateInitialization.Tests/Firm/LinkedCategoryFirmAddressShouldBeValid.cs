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
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement LinkedCategoryFirmAddressShouldBeValid
            => ArrangeMetadataElement
                .Config
                .Name(nameof(LinkedCategoryFirmAddressShouldBeValid))
                .Fact(
                    new Facts::Order { Id = 1, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(2) },
                    new Facts::OrderWorkflow { Id = 1, Step = 5 },

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, CategoryId = 2, FirmAddressId = 2 },
                    new Facts::FirmAddress { Id = 2 },
                    new Facts::FirmAddressCategory { FirmAddressId = 2, CategoryId = 2 },

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, CategoryId = 3, FirmAddressId = 3 },
                    new Facts::FirmAddress { Id = 3 })
                .Aggregate(
                    new Order { Id = 1, Start = MonthStart(1), End = MonthStart(2) },
                    new Order.CategoryNotBelongsToAddress { OrderId = 1, CategoryId = 3, FirmAddressId = 3, OrderPositionId = 1 })
                .Message(
                    new Messages::Version.ValidationResult
                        {
                            MessageParams = new MessageParams(
                                    new Reference<EntityTypeFirmAddress>(3),
                                    new Reference<EntityTypeCategory>(3),
                                    new Reference<EntityTypeOrder>(1),
                                    new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                        new Reference<EntityTypeOrderPosition>(1),
                                        new Reference<EntityTypePosition>(0)))
                                .ToXDocument(),
                            MessageType = (int)MessageTypeCode.LinkedCategoryFirmAddressShouldBeValid,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 1,
                        });
    }
}
