﻿using System.Collections.Generic;

using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.AdvertisementRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using Messages = NuClear.ValidationRules.Storage.Model.Messages;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;

// ReSharper disable once CheckNamespace
namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement AdvertisementMustPassReview
            => ArrangeMetadataElement
                .Config
                .Name(nameof(AdvertisementMustPassReview))
                .Fact(
                    new Facts::Order { Id = 1, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(2) },
                    new Facts::Position { Id = 100, ContentSales = 3 },

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 3, AdvertisementId = 5, PositionId = 100 },
                    new Facts::Advertisement { Id = 5, StateCode = 0 },

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 6, AdvertisementId = 8, PositionId = 100  },
                    new Facts::Advertisement { Id = 8, StateCode = 1 },

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 9, AdvertisementId = 11, PositionId = 100  },
                    new Facts::Advertisement { Id = 11, StateCode = 3 }
                )
                .Aggregate(
                    new Order { Id = 1, Start = MonthStart(1), End = MonthStart(2) },
                    new Order.AdvertisementFailedReview { OrderId = 1, AdvertisementId = 8, ReviewState = 1, AdvertisementIsOptional = false },
                    new Order.AdvertisementFailedReview { OrderId = 1, AdvertisementId = 11, ReviewState = 3, AdvertisementIsOptional = false  }
                )
                .Message(
                    new Messages::Version.ValidationResult
                    {
                        MessageParams =
                            new MessageParams(
                                    new Dictionary<string, object> { { "reviewState", 1 } },
                                    new Reference<EntityTypeOrder>(1),
                                    new Reference<EntityTypeAdvertisement>(8))
                                .ToXDocument(),

                        MessageType = (int)MessageTypeCode.AdvertisementMustPassReview,
                        PeriodStart = MonthStart(1),
                        PeriodEnd = MonthStart(2),
                        OrderId = 1,
                    },
                    new Messages::Version.ValidationResult
                        {
                            MessageParams =
                                new MessageParams(
                                                  new Dictionary<string, object> { { "reviewState", 3 } },
                                                  new Reference<EntityTypeOrder>(1),
                                                  new Reference<EntityTypeAdvertisement>(11))
                                    .ToXDocument(),

                            MessageType = (int)MessageTypeCode.AdvertisementShouldNotHaveComments,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 1,
                        }

                );
    }
}
