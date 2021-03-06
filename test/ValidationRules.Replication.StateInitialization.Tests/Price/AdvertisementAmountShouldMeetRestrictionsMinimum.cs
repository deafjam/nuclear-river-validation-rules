﻿using System;
using System.Collections.Generic;

using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Messages = NuClear.ValidationRules.Storage.Model.Messages;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement AdvertisementAmountShouldMeetRestrictionsMinimum
            => ArrangeMetadataElement
                .Config
                .Name(nameof(AdvertisementAmountShouldMeetRestrictionsMinimum))
                .Aggregate(
                    new Ruleset.AdvertisementAmountRestriction { Begin = MonthStart(1), End = DateTime.MaxValue, ProjectId = 13,
                        CategoryCode = 1, Min = 2, Max = 9 },

                    // Одобренный заказ на два месяца, поскольку он всего один - будет предупреждение, единичное для этого заказа и массовое.
                    new Order.AmountControlledPosition { OrderId = 1, CategoryCode = 1 },
                    new Order.OrderPeriod { OrderId = 1, ProjectId = 13, Start = MonthStart(1), End = MonthStart(2), Scope = 0 },
                    new Order.OrderPeriod { OrderId = 1, ProjectId = 13, Start = MonthStart(2), End = MonthStart(3), Scope = 0 },

                    // Заказ на утверждении - второй в этом периоде, единичное предупреждение для него не возникает.
                    new Order.AmountControlledPosition { OrderId = 2, CategoryCode = 1 },
                    new Order.OrderPeriod { OrderId = 2, ProjectId = 13, Start = MonthStart(1), End = MonthStart(2), Scope = -1 },

                    // Черновик - второй в этом периоде, единичное предупреждение для него не возникает.
                    new Order.AmountControlledPosition { OrderId = 3, CategoryCode = 1 },
                    new Order.OrderPeriod { OrderId = 3, ProjectId = 13, Start = MonthStart(2), End = MonthStart(3), Scope = 4 },

                    new Period { ProjectId = 13, Start = MonthStart(1), End = MonthStart(2) },
                    new Period { ProjectId = 13, Start = MonthStart(2), End = MonthStart(3) },
                    new Period { ProjectId = 13, Start = MonthStart(3), End = DateTime.MaxValue })
                .Message(
                    // Единичные предупреждения - только для опубликованного заказа (он не "видит" остальные)
                    new Messages::Version.ValidationResult
                        {
                            MessageParams =
                                        new MessageParams(
                                            new Dictionary<string, object> { { "min", 2 }, { "max", 9 }, { "count", 1 }, { "start", MonthStart(1) }, { "end", MonthStart(2) } },
                                            new Reference<EntityTypeOrder>(1),
                                            new Reference<EntityTypeNomenclatureCategory>(1)).ToXDocument(),
                            MessageType = (int)MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictions,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 1,
                        },
                    new Messages::Version.ValidationResult
                        {
                            MessageParams =
                                new MessageParams(
                                                  new Dictionary<string, object> { { "min", 2 }, { "max", 9 }, { "count", 1 }, { "start", MonthStart(2) }, { "end", MonthStart(3) } },
                                                  new Reference<EntityTypeOrder>(1),
                                                  new Reference<EntityTypeNomenclatureCategory>(1)).ToXDocument(),
                            MessageType = (int)MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictions,
                            PeriodStart = MonthStart(2),
                            PeriodEnd = MonthStart(3),
                            OrderId = 1,
                        },

                    // Массовые предупреждения - на все периоды (особенно на тот, где нет заказов)
                    new Messages::Version.ValidationResult
                        {
                            MessageParams =
                                new MessageParams(
                                                  new Dictionary<string, object> { { "min", 2 }, { "max", 9 }, { "count", 1 }, { "start", MonthStart(1) }, { "end", MonthStart(2) } },
                                                  new Reference<EntityTypeProject>(13),
                                                  new Reference<EntityTypeNomenclatureCategory>(1)).ToXDocument(),
                            MessageType = (int)MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictionsMass,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            ProjectId = 13,
                        },
                    new Messages::Version.ValidationResult
                        {
                            MessageParams =
                                new MessageParams(
                                                  new Dictionary<string, object> { { "min", 2 }, { "max", 9 }, { "count", 1 }, { "start", MonthStart(2) }, { "end", MonthStart(3) } },
                                                  new Reference<EntityTypeProject>(13),
                                                  new Reference<EntityTypeNomenclatureCategory>(1)).ToXDocument(),
                            MessageType = (int)MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictionsMass,
                            PeriodStart = MonthStart(2),
                            PeriodEnd = MonthStart(3),
                            ProjectId = 13,
                        },
                    new Messages::Version.ValidationResult
                        {
                            MessageParams =
                                new MessageParams(
                                                  new Dictionary<string, object> { { "min", 2 }, { "max", 9 }, { "count", 0 }, { "start", MonthStart(3) }, { "end", DateTime.MaxValue } },
                                                  new Reference<EntityTypeProject>(13),
                                                  new Reference<EntityTypeNomenclatureCategory>(1)).ToXDocument(),
                            MessageType = (int)MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictionsMass,
                            PeriodStart = MonthStart(3),
                            PeriodEnd = DateTime.MaxValue,
                            ProjectId = 13,
                        }
                    );
    }
}
