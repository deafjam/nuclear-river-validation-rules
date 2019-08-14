using System;
using System.Collections.Generic;

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
        private const int PositionsGroupMedia = 1;
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement OrderPositionSalesModelMustMatchCategorySalesModel
            => ArrangeMetadataElement
                .Config
                .Name(nameof(OrderPositionSalesModelMustMatchCategorySalesModel))
                .Fact(
                    // Заказ 1 с неправильным размещением в рубрике - есть ошибка
                    new Facts::Order { Id = 1, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(3) },
                    new Facts::OrderWorkflow { Id = 1 },
                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, PositionId = 1, CategoryId = 12 },
                    new Facts::Position { Id = 1, SalesModel = 2 },

                    // Заказ 2 с корректным размещением в рубрике - нет ошибки
                    new Facts::Order { Id = 2, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(3) },
                    new Facts::OrderWorkflow { Id = 2 },
                    new Facts::OrderPositionAdvertisement {OrderId = 2, OrderPositionId = 2, PositionId = 2, CategoryId = 12 },
                    new Facts::Position { Id = 2, SalesModel = 1 },

                    // Заказ 3 с неправильным размещением в рубрике, но позиция игнорирует модель продаж - нет ошибки
                    new Facts::Order { Id = 3, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(3) },
                    new Facts::OrderWorkflow { Id = 3 },
                    new Facts::OrderPositionAdvertisement {OrderId = 3, OrderPositionId = 3, PositionId = 3, CategoryId = 12 },
                    new Facts::PricePosition { Id = 3, PositionId = 3 },
                    new Facts::Position { Id = 3, SalesModel = 2, PositionsGroup = PositionsGroupMedia },

                    // Заказ 4 начинает размещение корректно, но в процессе меняется модель продаж - есть ошибка
                    new Facts::Order { Id = 4, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(8) },
                    new Facts::OrderWorkflow { Id = 4 },
                    new Facts::OrderPositionAdvertisement {OrderId = 4, OrderPositionId = 4, PositionId = 4, CategoryId = 12 },
                    new Facts::Position { Id = 4, SalesModel = 1 },

                    // Рубрика 12 в проекте 0 допускает размещение с моделью продаж 1 до 4-го месяца и моделью 2 позднее
                    new Facts::CategoryOrganizationUnit { CategoryId = 12 },
                    new Facts::Category { Id = 12, L3Id = 3, IsActiveNotDeleted = true },
                    new Facts::SalesModelCategoryRestriction { CategoryId = 12, Start = MonthStart(1), SalesModel = 1 },
                    new Facts::SalesModelCategoryRestriction { CategoryId = 12, Start = MonthStart(4), SalesModel = 2 },
                    new Facts::Project())
                .Aggregate(
                    // Заказ 1 с неправильным размещением в рубрике - есть ошибка
                    new Order { Id = 1, Start = MonthStart(1), End = MonthStart(3) },
                    new Order.CategoryAdvertisement { OrderId = 1, OrderPositionId = 1, PositionId = 1, CategoryId = 12, SalesModel = 2, IsSalesModelRestrictionApplicable = true },

                    // Заказ 2 с корректным размещением в рубрике - нет ошибки
                    new Order { Id = 2, Start = MonthStart(1), End = MonthStart(3) },
                    new Order.CategoryAdvertisement { OrderId = 2, OrderPositionId = 2, PositionId = 2, CategoryId = 12, SalesModel = 1, IsSalesModelRestrictionApplicable = true },

                    // Заказ 3 с неправильным размещением в рубрике, но позиция игнорирует модель продаж - нет ошибки
                    new Order { Id = 3, Start = MonthStart(1), End = MonthStart(3) },
                    new Order.CategoryAdvertisement { OrderId = 3, OrderPositionId = 3, PositionId = 3, CategoryId = 12, SalesModel = 2, IsSalesModelRestrictionApplicable = false },

                    // Заказ 4 начинает размещение корректно, но в процессе меняется модель продаж - есть ошибка
                    new Order { Id = 4, Start = MonthStart(1), End = MonthStart(8) },
                    new Order.CategoryAdvertisement { OrderId = 4, OrderPositionId = 4, PositionId = 4, CategoryId = 12, SalesModel = 1, IsSalesModelRestrictionApplicable = true },

                    new Project(),
                    new Project.Category { CategoryId = 12 },
                    new Project.SalesModelRestriction { CategoryId = 12, Start = MonthStart(1), End = MonthStart(4), SalesModel = 1 },
                    new Project.SalesModelRestriction { CategoryId = 12, Start = MonthStart(4), End = DateTime.MaxValue, SalesModel = 2 })
                .Message(
                    new Messages::Version.ValidationResult
                        {
                            MessageParams =
                                new MessageParams(
                                        new Dictionary<string, object> { { "start", MonthStart(1) } },
                                        new Reference<EntityTypeCategory>(12),
                                        new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                            new Reference<EntityTypeOrderPosition>(1),
                                            new Reference<EntityTypePosition>(1)),
                                        new Reference<EntityTypeOrder>(1),
                                        new Reference<EntityTypeProject>(0))
                                    .ToXDocument(),
                            MessageType = (int)MessageTypeCode.OrderPositionSalesModelMustMatchCategorySalesModel,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(3),
                            OrderId = 1,
                        },
                    new Messages::Version.ValidationResult
                        {
                            MessageParams =
                                new MessageParams(
                                        new Dictionary<string, object> { { "start", MonthStart(4) } },
                                        new Reference<EntityTypeCategory>(12),
                                        new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                            new Reference<EntityTypeOrderPosition>(4),
                                            new Reference<EntityTypePosition>(4)),
                                        new Reference<EntityTypeOrder>(4),
                                        new Reference<EntityTypeProject>(0))
                                    .ToXDocument(),
                            MessageType = (int)MessageTypeCode.OrderPositionSalesModelMustMatchCategorySalesModel,
                            PeriodStart = MonthStart(4),
                            PeriodEnd = MonthStart(8),
                            OrderId = 4,
                        });
    }
}
