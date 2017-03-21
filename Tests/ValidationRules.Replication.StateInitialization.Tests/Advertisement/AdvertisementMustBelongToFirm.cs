﻿using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Messages;

using Aggregates = NuClear.ValidationRules.Storage.Model.AdvertisementRules.Aggregates;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using Messages = NuClear.ValidationRules.Storage.Model.Messages;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement AdvertisementMustBelongToFirmPositive
            => ArrangeMetadataElement
                .Config
                .Name(nameof(AdvertisementMustBelongToFirmPositive))
                .Fact(
                    new Facts::Order { Id = 1, DestOrganizationUnitId = 2, BeginDistribution = FirstDayJan, EndDistributionPlan = FirstDayFeb, FirmId = 7},
                    new Facts::Project {Id = 3, OrganizationUnitId = 2},

                    new Facts::OrderPosition { Id = 4, OrderId = 1, },
                    new Facts::OrderPositionAdvertisement { OrderPositionId = 4, PositionId = 5, AdvertisementId = 6 },

                    new Facts::Position { Id = 5 },
                    new Facts::Advertisement { Id = 6, FirmId = 8, AdvertisementTemplateId = 9 }, // Фирмы в РМ и в заказе не совпадают
                    new Facts::AdvertisementTemplate { Id = 9, DummyAdvertisementId = -6 },
                    new Facts::Firm { Id = 7 }
                )
                .Aggregate(
                    new Aggregates::Order { Id = 1, ProjectId = 3, BeginDistributionDate = FirstDayJan, EndDistributionDatePlan = FirstDayFeb, FirmId = 7 },
                    new Aggregates::Order.AdvertisementMustBelongToFirm { OrderId = 1, OrderPositionId = 4, PositionId = 5, AdvertisementId = 6, FirmId = 7 },

                    new Aggregates::Advertisement { Id = 6, FirmId = 8 },
                    new Aggregates::Firm { Id = 7 }
                )
                .Message(
                    new Messages::Version.ValidationResult
                    {
                        MessageParams = new MessageParams(
                                new Reference<EntityTypeOrder>(1),
                                new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                    new Reference<EntityTypeOrderPosition>(4),
                                    new Reference<EntityTypePosition>(5)),
                                new Reference<EntityTypeAdvertisement>(6),
                                new Reference<EntityTypeFirm>(7)).ToXDocument(),
                        MessageType = (int)MessageTypeCode.AdvertisementMustBelongToFirm,
                        PeriodStart = FirstDayJan,
                        PeriodEnd = FirstDayFeb,
                        OrderId = 1,
                    }
                );

        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement AdvertisementMustBelongToFirmNegative
            => ArrangeMetadataElement
                .Config
                .Name(nameof(AdvertisementMustBelongToFirmNegative))
                .Fact(
                    new Facts::Order { Id = 1, DestOrganizationUnitId = 2, BeginDistribution = FirstDayJan, EndDistributionPlan = FirstDayFeb, FirmId = 7 },
                    new Facts::Project { Id = 3, OrganizationUnitId = 2 },

                    new Facts::OrderPosition { Id = 4, OrderId = 1, },
                    new Facts::OrderPositionAdvertisement { OrderPositionId = 4, PositionId = 5, AdvertisementId = 6 },

                    new Facts::Position { Id = 5 },
                    new Facts::Advertisement { Id = 6, FirmId = 7, AdvertisementTemplateId = 9 },
                    new Facts::AdvertisementTemplate { Id = 9, DummyAdvertisementId = -6 },
                    new Facts::Firm { Id = 7 }
                )
                .Aggregate(
                    new Aggregates::Order { Id = 1, ProjectId = 3, BeginDistributionDate = FirstDayJan, EndDistributionDatePlan = FirstDayFeb, FirmId = 7 },

                    new Aggregates::Advertisement { Id = 6, FirmId = 7 },
                    new Aggregates::Firm { Id = 7 }
                )
                .Message(
                );
    }
}
