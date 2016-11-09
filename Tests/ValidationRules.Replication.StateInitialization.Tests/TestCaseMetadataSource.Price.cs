﻿using System;

using NuClear.DataTest.Metamodel.Dsl;

using Aggregates = NuClear.ValidationRules.Storage.Model.PriceRules.Aggregates;
using Facts = NuClear.ValidationRules.Storage.Model.PriceRules.Facts;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // todo: по завршении работ с периодами добавить проверку связи прайса и города
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement Price
            => ArrangeMetadataElement.Config
                .Name(nameof(Price))
                .Fact(
                    new Facts::Price { Id = 1, BeginDate = DateTime.Parse("2012-12-12") },

                    // Position без ограничений
                    new Facts::PricePosition { Id = 1, PriceId = 1, PositionId = 2 },
                    new Facts::Position { Id = 2, CategoryCode = 101, IsControlledByAmount = false },

                    // Position с ограничениями
                    new Facts::PricePosition { Id = 2, PriceId = 1, PositionId = 3, MinAdvertisementAmount = 1, MaxAdvertisementAmount = 2 },
                    new Facts::Position { Id = 3, CategoryCode = 102, IsControlledByAmount = true },

                    // Некорректная Position с ограничениями
                    new Facts::PricePosition { Id = 3, PriceId = 1, PositionId = 4, MinAdvertisementAmount = null, MaxAdvertisementAmount = null },
                    new Facts::Position { Id = 4, CategoryCode = 103, IsControlledByAmount = true },

                    // associated
                    new Facts::AssociatedPosition { PositionId = 1, ObjectBindingType = 3, AssociatedPositionsGroupId = 1, Id = 1 },
                    new Facts::AssociatedPositionsGroup { Id = 1, PricePositionId = 1},

                    // denied
                    new Facts::DeniedPosition { PositionId = 1, PositionDeniedId = 2, ObjectBindingType = 3, PriceId = 1, Id = 1 }
                    )
                .Aggregate(
                    new Aggregates::Price { Id = 1, BeginDate = DateTime.Parse("2012-12-12") },

                    // ограничения
                    new Aggregates::AdvertisementAmountRestriction { CategoryCode = 102, PriceId = 1, Min = 1, Max = 2},
                    new Aggregates::AdvertisementAmountRestriction { CategoryCode = 103, PriceId = 1, Max = 2147483647, MissingMinimalRestriction = true }, // null for max means "unlimited", null for min means error

                    // сопутствующий хлам
                    new Aggregates::Period { Start = DateTime.Parse("2012-12-12"), End = DateTime.MaxValue },
                    new Aggregates::PricePeriod { PriceId = 1, Start = DateTime.Parse("2012-12-12") },

                    new Aggregates::Position { Id = 2, CategoryCode = 101 },
                    new Aggregates::Position { Id = 3, CategoryCode = 102 },
                    new Aggregates::Position { Id = 4, CategoryCode = 103 });

        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement PriceWithAssociatedPositionGroupOvercount
            => ArrangeMetadataElement.Config
                .Name(nameof(PriceWithAssociatedPositionGroupOvercount))
                .Fact(
                    new Facts::Price { Id = 1, BeginDate = DateTime.Parse("2012-12-12") },

                    // Position с ограничениями
                    new Facts::PricePosition { Id = 10, PriceId = 1, PositionId = 3 },
                    new Facts::Position { Id = 3, CategoryCode = 1 },

                    new Facts::AssociatedPositionsGroup { Id = 20, PricePositionId = 10 },
                    new Facts::AssociatedPositionsGroup { Id = 21, PricePositionId = 10 }
                    )
                .Aggregate(
                    new Aggregates::Price { Id = 1, BeginDate = DateTime.Parse("2012-12-12") },
                    new Aggregates::AssociatedPositionGroupOvercount { PriceId = 1, Count = 2, PricePositionId = 10 },

                    new Aggregates::Position { Id = 3, CategoryCode = 1 },
                    new Aggregates::PricePeriod { PriceId = 1, Start = DateTime.Parse("2012-12-12") },
                    new Aggregates::Period { Start = DateTime.Parse("2012-12-12"), End = DateTime.MaxValue }
                    );
    }
}