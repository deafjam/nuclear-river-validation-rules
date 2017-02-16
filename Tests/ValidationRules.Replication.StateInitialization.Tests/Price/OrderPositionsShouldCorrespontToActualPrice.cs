﻿using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Messages;

using Erm = NuClear.ValidationRules.Storage.Model.Erm;
using Aggregates = NuClear.ValidationRules.Storage.Model.PriceRules.Aggregates;
using Messages = NuClear.ValidationRules.Storage.Model.Messages;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement OrderPositionsShouldCorrespontToActualPrice
            => ArrangeMetadataElement
                .Config
                .Name(nameof(OrderPositionsShouldCorrespontToActualPrice))
                .Aggregate(
                    new Aggregates::Order { Id = 1 },
                    new Aggregates::Period.OrderPeriod { OrderId = 1, Start = FirstDayJan },
                    new Aggregates::Period.OrderPeriod { OrderId = 1, Start = FirstDayFeb },

                    new Aggregates::Order { Id = 2 },
                    new Aggregates::Period.OrderPeriod { OrderId = 2, Start = FirstDayFeb },

                    new Aggregates::Period { Start = FirstDayJan, End = FirstDayFeb },
                    new Aggregates::Period { Start = FirstDayFeb, End = FirstDayMar },

                    new Aggregates::Period.PricePeriod { Start = FirstDayFeb },

                    new Aggregates::Price())
                .Message(
                    new Messages::Version.ValidationResult
                        {
                            MessageParams = new MessageParams(new Reference<EntityTypeOrder>(1)).ToXDocument(),
                            MessageType = (int)MessageTypeCode.OrderPositionsShouldCorrespontToActualPrice,
                            Result = 3,
                            PeriodStart = FirstDayJan,
                            PeriodEnd = FirstDayMar,
                            OrderId = 1,
                        });
    }
}
