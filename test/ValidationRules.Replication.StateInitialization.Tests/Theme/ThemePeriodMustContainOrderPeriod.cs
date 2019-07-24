using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.ThemeRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using Messages = NuClear.ValidationRules.Storage.Model.Messages;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement ThemePeriodMustContainOrderPeriodPositive
            => ArrangeMetadataElement
                .Config
                .Name(nameof(ThemePeriodMustContainOrderPeriodPositive))
                .Fact(
                    new Facts::Order { Id = 1, DestProjectId = 3, AgileDistributionStartDate = FirstDayJan, AgileDistributionEndFactDate = FirstDayFeb},

                    new Facts::OrderPosition { Id = 4, OrderId = 1, },
                    new Facts::OrderPositionAdvertisement { OrderPositionId = 4, ThemeId = 5 },

                    new Facts::Theme { Id = 5, BeginDistribution = FirstDayFeb, EndDistribution = FirstDayFeb }
                )
                .Aggregate(
                    new Order { Id = 1, ProjectId = 3, Start = FirstDayJan, End = FirstDayFeb },
                    new Order.OrderTheme { OrderId = 1, ThemeId = 5 },

                    new Theme { Id = 5, BeginDistribution = FirstDayFeb, EndDistribution = FirstDayFeb }
                )
                .Message(
                    new Messages::Version.ValidationResult
                    {
                        MessageParams = new MessageParams(
                                new Reference<EntityTypeOrder>(1),
                                new Reference<EntityTypeTheme>(5)).ToXDocument(),
                        MessageType = (int)MessageTypeCode.ThemePeriodMustContainOrderPeriod,
                        PeriodStart = FirstDayJan,
                        PeriodEnd = FirstDayFeb,
                        OrderId = 1,
                    }
                );

        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement ThemePeriodMustContainOrderPeriodNegative
            => ArrangeMetadataElement
                .Config
                .Name(nameof(ThemePeriodMustContainOrderPeriodNegative))
                .Fact(
                    new Facts::Order { Id = 1, DestProjectId = 3, AgileDistributionStartDate = FirstDayJan, AgileDistributionEndFactDate = FirstDayFeb },

                    new Facts::OrderPosition { Id = 4, OrderId = 1, },
                    new Facts::OrderPositionAdvertisement { OrderPositionId = 4, ThemeId = 5 },

                    new Facts::Theme { Id = 5, BeginDistribution = FirstDayJan, EndDistribution = FirstDayFeb }
                )
                .Aggregate(
                    new Order { Id = 1, ProjectId = 3, Start = FirstDayJan, End = FirstDayFeb },
                    new Order.OrderTheme { OrderId = 1, ThemeId = 5 },

                    new Theme { Id = 5, BeginDistribution = FirstDayJan, EndDistribution = FirstDayFeb }
                )
                .Message(
                );
    }
}
