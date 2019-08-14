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
        private static ArrangeMetadataElement ThemeCategoryMustBeActiveAndNotDeleted_OneOrder
            => ArrangeMetadataElement
                .Config
                .Name(nameof(ThemeCategoryMustBeActiveAndNotDeleted_OneOrder))
                .Fact(
                    new Facts::Order { Id = 1, ProjectId = 3, AgileDistributionStartDate = FirstDayJan, AgileDistributionEndFactDate = FirstDayFeb},

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 4, ThemeId = 5 },

                    new Facts::Theme { Id = 5, BeginDistribution = FirstDayJan, EndDistribution = FirstDayFeb },
                    new Facts::Category { Id = 6, IsActiveNotDeleted = false },

                    new Facts::ThemeCategory { ThemeId = 5, CategoryId = 6 }
                )
                .Aggregate(
                    new Order { Id = 1, ProjectId = 3, Start = FirstDayJan, End = FirstDayFeb },
                    new Order.OrderTheme { OrderId = 1, ThemeId = 5 },

                    new Theme { Id = 5, BeginDistribution = FirstDayJan, EndDistribution = FirstDayFeb },
                    new Theme.InvalidCategory { ThemeId = 5, CategoryId = 6 }
                )
                .Message(
                    new Messages::Version.ValidationResult
                    {
                        MessageParams = new MessageParams(
                                new Reference<EntityTypeTheme>(5),
                                new Reference<EntityTypeCategory>(6)).ToXDocument(),
                        MessageType = (int)MessageTypeCode.ThemeCategoryMustBeActiveAndNotDeleted,
                        PeriodStart = FirstDayJan,
                        PeriodEnd = FirstDayFeb,
                        ProjectId = 3,
                    }
                );

        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement ThemeCategoryMustBeActiveAndNotDeleted_TwoOrders
            => ArrangeMetadataElement
                .Config
                .Name(nameof(ThemeCategoryMustBeActiveAndNotDeleted_TwoOrders))
                .Fact(
                    new Facts::Order { Id = 1, ProjectId = 3, AgileDistributionStartDate = FirstDayJan, AgileDistributionEndFactDate = FirstDayMar },
                    new Facts::Order { Id = 2, ProjectId = 3, AgileDistributionStartDate = FirstDayFeb, AgileDistributionEndFactDate = FirstDayApr },

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 4, ThemeId = 5 },
                    new Facts::OrderPositionAdvertisement {OrderId = 2, OrderPositionId = 5, ThemeId = 5 },

                    new Facts::Theme { Id = 5, BeginDistribution = FirstDayJan, EndDistribution = FirstDayApr },
                    new Facts::Category { Id = 6, IsActiveNotDeleted = false },

                    new Facts::ThemeCategory { ThemeId = 5, CategoryId = 6 }
                )
                .Aggregate(
                    new Order { Id = 1, ProjectId = 3, Start = FirstDayJan, End = FirstDayMar },
                    new Order { Id = 2, ProjectId = 3, Start = FirstDayFeb, End = FirstDayApr },
                    new Order.OrderTheme { OrderId = 1, ThemeId = 5 },
                    new Order.OrderTheme { OrderId = 2, ThemeId = 5 },

                    new Theme { Id = 5, BeginDistribution = FirstDayJan, EndDistribution = FirstDayApr },
                    new Theme.InvalidCategory { ThemeId = 5, CategoryId = 6 }
                )
                .Message(
                    new Messages::Version.ValidationResult
                        {
                            MessageParams = new MessageParams(
                                new Reference<EntityTypeTheme>(5),
                                new Reference<EntityTypeCategory>(6)).ToXDocument(),
                            MessageType = (int)MessageTypeCode.ThemeCategoryMustBeActiveAndNotDeleted,
                            PeriodStart = FirstDayJan,
                            PeriodEnd = FirstDayApr,
                            ProjectId = 3,
                        }
                );

        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement ThemeCategoryMustBeActiveAndNotDeletedNaegative
            => ArrangeMetadataElement
                .Config
                .Name(nameof(ThemeCategoryMustBeActiveAndNotDeletedNaegative))
                .Fact(
                    new Facts::Order { Id = 1, ProjectId = 3, AgileDistributionStartDate = FirstDayJan, AgileDistributionEndFactDate = FirstDayFeb },

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 4, ThemeId = 5 },

                    new Facts::Theme { Id = 5, BeginDistribution = FirstDayJan, EndDistribution = FirstDayFeb },
                    new Facts::Category { Id = 6, IsActiveNotDeleted = true },

                    new Facts::ThemeCategory { ThemeId = 5, CategoryId = 6 }
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
