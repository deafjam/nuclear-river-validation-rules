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
                    new Facts::Order { Id = 1, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(2), AgileDistributionEndPlanDate = MonthStart(2), WorkflowStep = 1 },
                    new Facts::Order { Id = 2, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(2), AgileDistributionEndPlanDate = MonthStart(2), WorkflowStep = 2 },
                    new Facts::Order { Id = 3, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(2), AgileDistributionEndPlanDate = MonthStart(2), WorkflowStep = 5 },
                    new Facts::Order { Id = 4, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndFactDate = MonthStart(2), AgileDistributionEndPlanDate = MonthStart(3), WorkflowStep = 4 })
                .Aggregate(
                    new Order.OrderPeriod { OrderId = 1, Begin = MonthStart(1), End = MonthStart(2), Scope = 1 },
                    new Order.OrderPeriod { OrderId = 2, Begin = MonthStart(1), End = MonthStart(2), Scope = -1 },
                    new Order.OrderPeriod { OrderId = 3, Begin = MonthStart(1), End = MonthStart(2), Scope = 0 },
                    new Order.OrderPeriod { OrderId = 4, Begin = MonthStart(1), End = MonthStart(2), Scope = 0 },
                    new Order.OrderPeriod { OrderId = 4, Begin = MonthStart(2), End = MonthStart(3), Scope = 4 });

        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement Period
            => ArrangeMetadataElement
                .Config
                .Name(nameof(Period))
                .Fact(
                    new Facts::Price { Id = 1, ProjectId = 2, BeginDate = MonthStart(1) },
                    new Facts::Price { Id = 2, ProjectId = 2, BeginDate = MonthStart(5) },
                    new Facts::Price { Id = 3, ProjectId = 1, BeginDate = MonthStart(3) },

                    new Facts::Project { Id = 1, OrganizationUnitId = 1 },
                    new Facts::Project { Id = 2, OrganizationUnitId = 2 },

                    new Facts::Order { Id = 1, DestOrganizationUnitId = 2, AgileDistributionStartDate = MonthStart(2), AgileDistributionEndFactDate = MonthStart(3), AgileDistributionEndPlanDate = MonthStart(4) },
                    new Facts::Order { Id = 3, DestOrganizationUnitId = 1, AgileDistributionStartDate = MonthStart(5), AgileDistributionEndFactDate = MonthStart(7), AgileDistributionEndPlanDate = MonthStart(7) })
                .Aggregate(
                    new Period { Start = MonthStart(1), End = MonthStart(2) },
                    new Period { Start = MonthStart(2), End = MonthStart(3) },
                    new Period { Start = MonthStart(3), End = MonthStart(4) },
                    new Period { Start = MonthStart(4), End = MonthStart(5) },
                    new Period { Start = MonthStart(5), End = MonthStart(7) },
                    new Period { Start = MonthStart(7), End = DateTime.MaxValue });
    }
}
