using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.AdvertisementRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;

// ReSharper disable once CheckNamespace
namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement OrderPositionAdvertisementMustHaveOptionalAdvertisementPositive
            => ArrangeMetadataElement
                .Config
                .Name(nameof(OrderPositionAdvertisementMustHaveOptionalAdvertisementPositive))
                .Fact(
                      new Facts::Order { Id = 1, AgileDistributionStartDate = FirstDayJan, AgileDistributionEndPlanDate = FirstDayFeb },

                      new Facts::OrderPosition { Id = 4, OrderId = 1, PricePositionId = 4 },
                      new Facts::PricePosition { Id = 4, PositionId = 5, IsActiveNotDeleted = true },
                      new Facts::Position { Id = 5, ContentSales = 2 },

                      new Facts::OrderPositionAdvertisement { OrderPositionId = 4, PositionId = 5, AdvertisementId = null }
                     )
                .Aggregate(
                           new Order { Id = 1, Start = FirstDayJan, End = FirstDayFeb },
                           new Order.MissingAdvertisementReference { OrderId = 1, OrderPositionId = 4, CompositePositionId = 5, PositionId = 5, AdvertisementIsOptional = true}
                          )
                .Message(
                         new Version.ValidationResult
                             {
                                 MessageParams = new MessageParams(
                                                                   new Reference<EntityTypeOrderPosition>(4,
                                                                                                          new Reference<EntityTypeOrder>(1),
                                                                                                          new Reference<EntityTypePosition>(5)),
                                                                   new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                                                                                                       new Reference<EntityTypeOrderPosition>(4),
                                                                                                                       new Reference<EntityTypePosition>(5))).ToXDocument(),
                                 MessageType = (int)MessageTypeCode.OrderPositionAdvertisementMustHaveOptionalAdvertisement,
                                 PeriodStart = FirstDayJan,
                                 PeriodEnd = FirstDayFeb,
                                 OrderId = 1,
                             }
                        );
    }
}
