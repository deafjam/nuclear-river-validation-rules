using System;

using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.ProjectRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using Messages = NuClear.ValidationRules.Storage.Model.Messages;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement OrderPositionCostPerClickMustBeSpecifiedPositive
            => ArrangeMetadataElement
                .Config
                .Name(nameof(OrderPositionCostPerClickMustBeSpecifiedPositive))
                .Fact(
                    // Заказ с позицией с покликовой моделью, но без ставки - есть ошибка
                    new Facts::Order { Id = 1, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(3) },
                    new Facts::OrderPosition { Id = 1, OrderId = 2, PricePositionId = 5 },
                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, CategoryId = 12, PositionId = 4 },

                    // Заказ с позицией с обычной моделью - нет ошибки
                    new Facts::Order { Id = 2, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(3) },
                    new Facts::OrderPosition { Id = 2, OrderId = 2, PricePositionId = 5 },
                    new Facts::OrderPositionAdvertisement {OrderId = 2, OrderPositionId = 2, CategoryId = 12, PositionId = 5 },

                    // Заказ с позицией с покликовой моделью, со ставкой - нет ошибки
                    new Facts::Order { Id = 3, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(3) },
                    new Facts::OrderPosition { Id = 3, OrderId = 3, PricePositionId = 4 },
                    new Facts::OrderPositionAdvertisement {OrderId = 3, OrderPositionId = 3, CategoryId = 12, PositionId = 4 },
                    new Facts::OrderPositionCostPerClick { OrderPositionId = 3, CategoryId = 12 },

                    new Facts::PricePosition { Id = 4, PositionId = 4 },
                    new Facts::PricePosition { Id = 5, PositionId = 5 },
                    new Facts::Position { Id = 4, SalesModel = 12 },
                    new Facts::Position { Id = 5, SalesModel = 11 },
                    new Facts::Category { Id = 12, IsActiveNotDeleted = true },
                    new Facts::CategoryOrganizationUnit { CategoryId = 12 },
                    new Facts::CostPerClickCategoryRestriction { Start = MonthStart(1), CategoryId = 12 },
                    new Facts::Project())
                .Aggregate(
                    // Заказ с позицией с покликовой моделью, но без ставки - есть ошибка
                    new Order { Id = 1, Start = MonthStart(1), End = MonthStart(3) },
                    new Order.CategoryAdvertisement { OrderId = 1, OrderPositionId = 1, PositionId = 4, CategoryId = 12, SalesModel = 12 },

                    // Заказ с позицией с обычной моделью - нет ошибки
                    new Order { Id = 2, Start = MonthStart(1), End = MonthStart(3) },
                    new Order.CategoryAdvertisement { OrderId = 2, OrderPositionId = 2, PositionId = 5, CategoryId = 12, SalesModel = 11 },

                    // Заказ с позицией с покликовой моделью, со ставкой - нет ошибки
                    new Order { Id = 3, Start = MonthStart(1), End = MonthStart(3) },
                    new Order.CategoryAdvertisement { OrderId = 3, OrderPositionId = 3, PositionId = 4, CategoryId = 12, SalesModel = 12 },
                    new Order.CostPerClickAdvertisement { OrderId = 3, OrderPositionId = 3, PositionId = 4, CategoryId = 12 },

                    new Project.Category { CategoryId = 12 },
                    new Project.CostPerClickRestriction { CategoryId = 12, Start = MonthStart(1), End = DateTime.MaxValue })
                .Message(
                    new Messages::Version.ValidationResult
                        {
                            MessageParams =
                                new MessageParams(
                                    new Reference<EntityTypeCategory>(12),
                                    new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                        new Reference<EntityTypeOrderPosition>(1),
                                        new Reference<EntityTypePosition>(4)),
                                    new Reference<EntityTypeOrder>(1)).ToXDocument(),
                            MessageType = (int)MessageTypeCode.OrderPositionCostPerClickMustBeSpecified,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(3),
                            OrderId = 1,
                        });
    }
}
