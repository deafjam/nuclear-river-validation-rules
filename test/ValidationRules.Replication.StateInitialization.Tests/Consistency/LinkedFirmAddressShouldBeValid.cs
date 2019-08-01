﻿using System.Collections.Generic;
using System.Linq;

using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.ConsistencyRules;
using NuClear.ValidationRules.Storage.Model.Facts;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using Messages = NuClear.ValidationRules.Storage.Model.Messages;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;
using Order = NuClear.ValidationRules.Storage.Model.Aggregates.ConsistencyRules.Order;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement LinkedFirmAddressShouldBeValid
            => ArrangeMetadataElement
                .Config
                .Name(nameof(LinkedFirmAddressShouldBeValid))
                .Fact(
                    new Facts::Order { Id = 1, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(2), FirmId = 1 },

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, FirmAddressId = 1, PositionId = 1 },
                    new Facts::FirmAddress { Id = 1, FirmId = 2 },

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, FirmAddressId = 2, PositionId = 1 },
                    new Facts::FirmAddressInactive { Id = 2, IsDeleted = true},

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, FirmAddressId = 3, PositionId = 1 },
                    new Facts::FirmAddressInactive { Id = 3 },

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, FirmAddressId = 4, PositionId = 1 },
                    new Facts::FirmAddressInactive { Id = 4, IsActive = true, IsClosedForAscertainment = true },

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, FirmAddressId = 5, PositionId = 2 },
                    new Facts::FirmAddress { Id = 5, FirmId = 1, EntranceCode = 1, BuildingPurposeCode = 1},

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, FirmAddressId = 6, PositionId = 2 },
                    new Facts::FirmAddress { Id = 6, FirmId = 1, EntranceCode = null, BuildingPurposeCode = null },

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, FirmAddressId = 7, PositionId = 2 },
                    new Facts::FirmAddress { Id = 7, FirmId = 1, EntranceCode = 1, BuildingPurposeCode = FirmAddress.InvalidBuildingPurposeCodesForPoi.First()},

                    new Facts::OrderPositionAdvertisement {OrderId = 1, OrderPositionId = 1, FirmAddressId = 8, PositionId = 3},
                    new Facts::FirmAddressInactive { Id = 8 },

                    new Facts::Position { Id = 1 },
                    new Facts::Position { Id = 2, CategoryCode = Position.CategoryCodesPoiAddressCheck.First() },
                    new Facts::Position { Id = 3, CategoryCode = Position.CategoryCodePartnerAdvertisingAddress, BindingObjectType = Position.BindingObjectTypeAddressMultiple })
                .Aggregate(
                    new Order { Id = 1, Start = MonthStart(1), End = MonthStart(2) },
                    new Order.InvalidFirmAddress { OrderId = 1, FirmAddressId = 1, OrderPositionId = 1, PositionId = 1, State = InvalidFirmAddressState.NotBelongToFirm },
                    new Order.InvalidFirmAddress { OrderId = 1, FirmAddressId = 2, OrderPositionId = 1, PositionId = 1, State = InvalidFirmAddressState.Deleted },
                    new Order.InvalidFirmAddress { OrderId = 1, FirmAddressId = 3, OrderPositionId = 1, PositionId = 1, State = InvalidFirmAddressState.NotActive },
                    new Order.InvalidFirmAddress { OrderId = 1, FirmAddressId = 4, OrderPositionId = 1, PositionId = 1, State = InvalidFirmAddressState.ClosedForAscertainment },
                    new Order.InvalidFirmAddress { OrderId = 1, FirmAddressId = 6, OrderPositionId = 1, PositionId = 2, State = InvalidFirmAddressState.MissingEntrance },
                    new Order.InvalidFirmAddress { OrderId = 1, FirmAddressId = 7, OrderPositionId = 1, PositionId = 2, State = InvalidFirmAddressState.InvalidBuildingPurpose },
                    new Order.InvalidFirmAddress { OrderId = 1, FirmAddressId = 8, OrderPositionId = 1, PositionId = 3, State = InvalidFirmAddressState.NotActive, IsPartnerAddress = true })
                .Message(
                    new Messages::Version.ValidationResult
                        {
                            MessageParams = new MessageParams(
                                    new Dictionary<string, object> { { "invalidFirmAddressState", (int)InvalidFirmAddressState.NotBelongToFirm }, {"isPartnerAddress", false} },
                                    new Reference<EntityTypeFirmAddress>(1),
                                    new Reference<EntityTypeOrder>(1),
                                    new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                        new Reference<EntityTypeOrderPosition>(1),
                                        new Reference<EntityTypePosition>(1)))
                                .ToXDocument(),
                            MessageType = (int)MessageTypeCode.LinkedFirmAddressShouldBeValid,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 1,
                        },
                    new Messages::Version.ValidationResult
                        {
                            MessageParams = new MessageParams(
                                    new Dictionary<string, object> { { "invalidFirmAddressState", (int)InvalidFirmAddressState.Deleted }, {"isPartnerAddress", false} },
                                    new Reference<EntityTypeFirmAddress>(2),
                                    new Reference<EntityTypeOrder>(1),
                                    new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                        new Reference<EntityTypeOrderPosition>(1),
                                        new Reference<EntityTypePosition>(1)))
                                .ToXDocument(),
                            MessageType = (int)MessageTypeCode.LinkedFirmAddressShouldBeValid,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 1,
                        },

                    new Messages::Version.ValidationResult
                        {
                            MessageParams = new MessageParams(
                                    new Dictionary<string, object> { { "invalidFirmAddressState", (int)InvalidFirmAddressState.NotActive }, {"isPartnerAddress", false} },
                                    new Reference<EntityTypeFirmAddress>(3),
                                    new Reference<EntityTypeOrder>(1),
                                    new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                        new Reference<EntityTypeOrderPosition>(1),
                                        new Reference<EntityTypePosition>(1)))
                                .ToXDocument(),
                            MessageType = (int)MessageTypeCode.LinkedFirmAddressShouldBeValid,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 1,
                        },

                    new Messages::Version.ValidationResult
                        {
                            MessageParams = new MessageParams(
                                    new Dictionary<string, object> { { "invalidFirmAddressState", (int)InvalidFirmAddressState.ClosedForAscertainment }, {"isPartnerAddress", false} },
                                    new Reference<EntityTypeFirmAddress>(4),
                                    new Reference<EntityTypeOrder>(1),
                                    new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                        new Reference<EntityTypeOrderPosition>(1),
                                        new Reference<EntityTypePosition>(1)))
                                .ToXDocument(),
                            MessageType = (int)MessageTypeCode.LinkedFirmAddressShouldBeValid,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 1,
                        },
                    new Messages::Version.ValidationResult
                        {
                            MessageParams = new MessageParams(
                                                            new Dictionary<string, object> { { "invalidFirmAddressState", (int)InvalidFirmAddressState.MissingEntrance }, {"isPartnerAddress", false} },
                                                            new Reference<EntityTypeFirmAddress>(6),
                                                            new Reference<EntityTypeOrder>(1),
                                                            new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                                                                                                new Reference<EntityTypeOrderPosition>(1),
                                                                                                                new Reference<EntityTypePosition>(2)))
                                .ToXDocument(),
                            MessageType = (int)MessageTypeCode.LinkedFirmAddressShouldBeValid,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 1,
                        },
                    new Messages::Version.ValidationResult
                        {
                            MessageParams = new MessageParams(
                                                            new Dictionary<string, object> { { "invalidFirmAddressState", (int)InvalidFirmAddressState.InvalidBuildingPurpose }, {"isPartnerAddress", false} },
                                                            new Reference<EntityTypeFirmAddress>(7),
                                                            new Reference<EntityTypeOrder>(1),
                                                            new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                                                                                                new Reference<EntityTypeOrderPosition>(1),
                                                                                                                new Reference<EntityTypePosition>(2)))
                                .ToXDocument(),
                            MessageType = (int)MessageTypeCode.LinkedFirmAddressShouldBeValid,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 1,
                        },
                    new Messages::Version.ValidationResult
                        {
                            MessageParams = new MessageParams(
                                                            new Dictionary<string, object> { { "invalidFirmAddressState", (int)InvalidFirmAddressState.NotActive }, {"isPartnerAddress", true} },
                                                            new Reference<EntityTypeFirmAddress>(8),
                                                            new Reference<EntityTypeOrder>(1),
                                                            new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                                                                                                new Reference<EntityTypeOrderPosition>(1),
                                                                                                                new Reference<EntityTypePosition>(3)))
                                .ToXDocument(),
                            MessageType = (int)MessageTypeCode.LinkedFirmAddressShouldBeValid,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 1,
                        });
    }
}
