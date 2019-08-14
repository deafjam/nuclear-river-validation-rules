using System;

using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement OrderPeriod
            => ArrangeMetadataElement
                .Config
                .Name(nameof(OrderPeriod))
                .Fact(
                    new Facts::Order { Id = 1, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(2), AgileDistributionEndPlanDate = MonthStart(2)},
                    new Facts::OrderWorkflow { Id = 1, Step = 1 },
                    new Facts::Order { Id = 2, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(2), AgileDistributionEndPlanDate = MonthStart(2)},
                    new Facts::OrderWorkflow { Id = 2, Step = 2 },
                    new Facts::Order { Id = 3, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(2), AgileDistributionEndPlanDate = MonthStart(2)},
                    new Facts::OrderWorkflow { Id = 3, Step = 5 },
                    new Facts::Order { Id = 4, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(2), AgileDistributionEndPlanDate = MonthStart(3)},
                    new Facts::OrderWorkflow { Id = 4, Step = 4 })
                .Aggregate(
                    new Order.OrderPeriod { OrderId = 1, Start = MonthStart(1), End = MonthStart(2), Scope = 1 },
                    new Order.OrderPeriod { OrderId = 2, Start = MonthStart(1), End = MonthStart(2), Scope = -1 },
                    new Order.OrderPeriod { OrderId = 3, Start = MonthStart(1), End = MonthStart(2), Scope = 0 },
                    new Order.OrderPeriod { OrderId = 4, Start = MonthStart(1), End = MonthStart(2), Scope = 0 },
                    new Order.OrderPeriod { OrderId = 4, Start = MonthStart(2), End = MonthStart(3), Scope = 4 });

        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement Period
            => ArrangeMetadataElement
                .Config
                .Name(nameof(Period))
                .Fact(
                    new Facts::Order { Id = 1, ProjectId = 2, AgileDistributionStartDate = MonthStart(2), AgileDistributionEndFactDate = MonthStart(3), AgileDistributionEndPlanDate = MonthStart(4) },
                    new Facts::Order { Id = 3, ProjectId = 1, AgileDistributionStartDate = MonthStart(5), AgileDistributionEndFactDate = MonthStart(7), AgileDistributionEndPlanDate = MonthStart(7) })
                .Aggregate(
                    new Period { ProjectId = 1, Start = DateTime.MinValue, End = MonthStart(5) },
                    new Period { ProjectId = 1, Start = MonthStart(5), End = MonthStart(7) },
                    new Period { ProjectId = 1, Start = MonthStart(7), End = DateTime.MaxValue },
                    
                    new Period { ProjectId = 2, Start = DateTime.MinValue, End = MonthStart(2) },
                    new Period { ProjectId = 2, Start = MonthStart(2), End = MonthStart(3) },
                    new Period { ProjectId = 2, Start = MonthStart(3), End = MonthStart(4) },
                    new Period { ProjectId = 2, Start = MonthStart(4), End = DateTime.MaxValue });
    }
}
