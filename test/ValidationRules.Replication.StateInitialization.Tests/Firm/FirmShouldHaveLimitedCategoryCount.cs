using System.Collections.Generic;
using System.Linq;

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
        private static ArrangeMetadataElement FirmShouldHaveLimitedCategoryCountAggregates
            => ArrangeMetadataElement
                .Config
                .Name(nameof(FirmShouldHaveLimitedCategoryCountAggregates))
                .Fact(
                    new Facts::Firm { Id = 1 },
                    new Facts::Order { Id = 1, FirmId = 1, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(3), AgileDistributionEndPlanDate = MonthStart(4) },
                    new Facts::OrderWorkflow {Id = 1, Step = 4},
                    new Facts::OrderItem { OrderId = 1, OrderPositionId = 1, CategoryId = 5 },

                    new Facts::Order { Id = 2, FirmId = 1, AgileDistributionStartDate = MonthStart(3), AgileDistributionEndFactDate = MonthStart(5), AgileDistributionEndPlanDate = MonthStart(5)},
                    new Facts::OrderWorkflow {Id = 2, Step = 5},
                    new Facts::OrderItem { OrderId = 2, OrderPositionId = 21, CategoryId = 5 },
                    new Facts::OrderItem { OrderId = 2, OrderPositionId = 22, CategoryId = 6 },

                    new Facts::Firm { Id = 2 },
                    new Facts::Order { Id = 3, FirmId = 2, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(5), AgileDistributionEndPlanDate = MonthStart(5)},
                    new Facts::OrderWorkflow {Id = 3, Step = 1},
                    new Facts::OrderItem { OrderId = 3, OrderPositionId = 3, CategoryId = 5 },
                    new Facts::Order { Id = 4, FirmId = 2, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(5), AgileDistributionEndPlanDate = MonthStart(5)},
                    new Facts::OrderWorkflow {Id = 4, Step = 1},
                    new Facts::OrderItem { OrderId = 4, OrderPositionId = 4, CategoryId = 5 })
                .Aggregate(
                    // Периоды строятся по 
                    new Firm.CategoryPurchase { FirmId = 1, CategoryId = 5, Start = MonthStart(1), End = MonthStart(3), Scope = 0 },
                    new Firm.CategoryPurchase { FirmId = 1, CategoryId = 5, Start = MonthStart(3), End = MonthStart(4), Scope = 1 },
                    new Firm.CategoryPurchase { FirmId = 1, CategoryId = 5, Start = MonthStart(3), End = MonthStart(4), Scope = 0 },
                    new Firm.CategoryPurchase { FirmId = 1, CategoryId = 5, Start = MonthStart(4), End = MonthStart(5), Scope = 0 },
                    new Firm.CategoryPurchase { FirmId = 1, CategoryId = 6, Start = MonthStart(3), End = MonthStart(4), Scope = 0 },
                    new Firm.CategoryPurchase { FirmId = 1, CategoryId = 6, Start = MonthStart(4), End = MonthStart(5), Scope = 0 },

                    // Периоды для разных фирм не зависят друг от друга. Рубрики могут дубливаться в разных Scope.
                    new Firm.CategoryPurchase { FirmId = 2, CategoryId = 5, Start = MonthStart(1), End = MonthStart(5), Scope = 3 },
                    new Firm.CategoryPurchase { FirmId = 2, CategoryId = 5, Start = MonthStart(1), End = MonthStart(5), Scope = 4 });


        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement FirmShouldHaveLimitedCategoryCountMessages
            => ArrangeMetadataElement
                .Config
                .Name(nameof(FirmShouldHaveLimitedCategoryCountMessages))
                .Aggregate(
                    new Order { Id = 1, FirmId = 1, Start = MonthStart(1), End = MonthStart(3) },
                    new Order { Id = 2, FirmId = 1, Start = MonthStart(2), End = MonthStart(4) })
                .Aggregate(Enumerable.Range(1, 15).Select(i => new Firm.CategoryPurchase { FirmId = 1, CategoryId = i, Start = MonthStart(1), End = MonthStart(2) }).ToArray())
                .Aggregate(Enumerable.Range(1, 27).Select(i => new Firm.CategoryPurchase { FirmId = 1, CategoryId = i, Start = MonthStart(2), End = MonthStart(3) }).ToArray())
                .Aggregate(Enumerable.Range(13, 15).Select(i => new Firm.CategoryPurchase { FirmId = 1, CategoryId = i, Start = MonthStart(3), End = MonthStart(4) }).ToArray())
                .Message(
                    new Messages::Version.ValidationResult
                    {
                        MessageParams = new MessageParams(
                                    new Dictionary<string, object> { { "count", 27 }, { "allowed", 20 } },
                                    new Reference<EntityTypeFirm>(1)).ToXDocument(),
                        MessageType = (int)MessageTypeCode.FirmShouldHaveLimitedCategoryCount,
                        PeriodStart = MonthStart(2),
                        PeriodEnd = MonthStart(3),
                        OrderId = 1,
                    },
                    new Messages::Version.ValidationResult
                    {
                        MessageParams = new MessageParams(
                                    new Dictionary<string, object> { { "count", 27 }, { "allowed", 20 } },
                                    new Reference<EntityTypeFirm>(1)).ToXDocument(),
                        MessageType = (int)MessageTypeCode.FirmShouldHaveLimitedCategoryCount,
                        PeriodStart = MonthStart(2),
                        PeriodEnd = MonthStart(3),
                        OrderId = 2,
                    });
    }
}
