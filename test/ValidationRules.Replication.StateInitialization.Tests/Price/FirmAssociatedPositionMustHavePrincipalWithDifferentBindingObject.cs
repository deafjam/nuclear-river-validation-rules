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
        private static ArrangeMetadataElement FirmAssociatedPositionMustHavePrincipalWithDifferentBindingObject
            => ArrangeMetadataElement
                .Config
                .Name(nameof(FirmAssociatedPositionMustHavePrincipalWithDifferentBindingObject))
                .Aggregate(
                    // Есть основная позиция, есть сопутствующая с отличающимся объектом привязки - нет ошибки
                    new Firm { Id = 1 },
                    new Firm.FirmPosition { FirmId = 1, OrderPositionId = 1, Start = MonthStart(1), End = MonthStart(2), PackagePositionId = 2, ItemPositionId = 2, Category1Id = 1, Category3Id = 2 },
                    new Firm.FirmPosition { FirmId = 1, OrderPositionId = 2, Start = MonthStart(1), End = MonthStart(2), PackagePositionId = 2, ItemPositionId = 3, Category1Id = 1, Category3Id = 3 },

                    new Firm.FirmAssociatedPosition { FirmId = 1, OrderPositionId = 1, PackagePositionId = 2, ItemPositionId = 2, BindingType = 3, PrincipalPositionId = 3 },

                    // Есть основная позиция, есть сопутствующая с совпадающим объектом привязки - есть ошибка
                    new Firm { Id = 2 },
                    new Firm.FirmPosition { FirmId = 2, OrderPositionId = 3, Start = MonthStart(1), End = MonthStart(2), PackagePositionId = 2, ItemPositionId = 2, Category1Id = 1, Category3Id = 2 },
                    new Firm.FirmPosition { FirmId = 2, OrderPositionId = 4, Start = MonthStart(1), End = MonthStart(2), PackagePositionId = 3, ItemPositionId = 3, Category1Id = 1, Category3Id = 2 },

                    new Firm.FirmAssociatedPosition { FirmId = 2, OrderPositionId = 3, PackagePositionId = 2, ItemPositionId = 2, BindingType = 3, PrincipalPositionId = 3 },

                    // Есть только сопутствующая - ошибка другого типа
                    new Firm { Id = 3 },
                    new Firm.FirmPosition { FirmId = 3, OrderPositionId = 5, Start = MonthStart(1), End = MonthStart(2), PackagePositionId = 2, ItemPositionId = 2, Category1Id = 1, Category3Id = 2 },

                    new Firm.FirmAssociatedPosition { FirmId = 3, OrderPositionId = 5, PackagePositionId = 2, ItemPositionId = 2, BindingType = 3, PrincipalPositionId = 3 },

                    // Есть две основных - с совпадающим и отличающимся объектами привязки - ошибка должна быть (а у нас сейчас нету)
                    new Firm { Id = 4 },
                    new Firm.FirmPosition { FirmId = 4, OrderPositionId = 6, Start = MonthStart(1), End = MonthStart(2), PackagePositionId = 2, ItemPositionId = 2, Category1Id = 1, Category3Id = 2 },
                    new Firm.FirmPosition { FirmId = 4, OrderPositionId = 7, Start = MonthStart(1), End = MonthStart(2), PackagePositionId = 3, ItemPositionId = 3, Category1Id = 1, Category3Id = 2 },
                    new Firm.FirmPosition { FirmId = 4, OrderPositionId = 8, Start = MonthStart(1), End = MonthStart(2), PackagePositionId = 3, ItemPositionId = 3, Category1Id = 1, Category3Id = 3 },

                    new Firm.FirmAssociatedPosition { FirmId = 4, OrderPositionId = 6, PackagePositionId = 2, ItemPositionId = 2, BindingType = 3, PrincipalPositionId = 3 },

                    // Есть две основных - без учёта и с совпадающим объектами привязки - ошибка есть // должна быть (а у нас сейчас нету)
                    new Firm { Id = 5 },
                    new Firm.FirmPosition { FirmId = 5, OrderPositionId =  9, Start = MonthStart(1), End = MonthStart(2), PackagePositionId = 2, ItemPositionId = 2, Category1Id = 1, Category3Id = 2 },
                    new Firm.FirmPosition { FirmId = 5, OrderPositionId = 10, Start = MonthStart(1), End = MonthStart(2), PackagePositionId = 3, ItemPositionId = 3, Category1Id = 1, Category3Id = 2 },
                    new Firm.FirmPosition { FirmId = 5, OrderPositionId = 11, Start = MonthStart(1), End = MonthStart(2), PackagePositionId = 4, ItemPositionId = 4 },

                    new Firm.FirmAssociatedPosition { FirmId = 5, OrderPositionId = 9, PackagePositionId = 2, ItemPositionId = 2, BindingType = 3, PrincipalPositionId = 3 },
                    new Firm.FirmAssociatedPosition { FirmId = 5, OrderPositionId = 9, PackagePositionId = 2, ItemPositionId = 2, BindingType = 2, PrincipalPositionId = 4 })
                .Message(
                    new Messages::Version.ValidationResult
                        {
                            MessageParams =
                                new MessageParams(
                                        new Reference<EntityTypeOrderPosition>(3,
                                            new Reference<EntityTypeOrder>(0),
                                            new Reference<EntityTypePosition>(2),
                                            new Reference<EntityTypePosition>(2)),

                                        new Reference<EntityTypeOrderPosition>(4,
                                            new Reference<EntityTypeOrder>(0),
                                            new Reference<EntityTypePosition>(3),
                                            new Reference<EntityTypePosition>(3)))
                                    .ToXDocument(),
                            MessageType = (int)MessageTypeCode.FirmAssociatedPositionMustHavePrincipalWithDifferentBindingObject,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 0,
                        },

                    new Messages::Version.ValidationResult
                        {
                            MessageParams =
                                new MessageParams(
                                        new Reference<EntityTypeOrderPosition>(5,
                                            new Reference<EntityTypeOrder>(0),
                                            new Reference<EntityTypePosition>(2),
                                            new Reference<EntityTypePosition>(2)))
                                    .ToXDocument(),
                            MessageType = (int)MessageTypeCode.FirmAssociatedPositionMustHavePrincipal,
                            PeriodStart = MonthStart(1),
                            PeriodEnd = MonthStart(2),
                            OrderId = 0,
                        },

                    new Messages::Version.ValidationResult
                    {
                        MessageParams =
                                new MessageParams(
                                        new Reference<EntityTypeOrderPosition>(6,
                                            new Reference<EntityTypeOrder>(0),
                                            new Reference<EntityTypePosition>(2),
                                            new Reference<EntityTypePosition>(2)),

                                        new Reference<EntityTypeOrderPosition>(7,
                                            new Reference<EntityTypeOrder>(0),
                                            new Reference<EntityTypePosition>(3),
                                            new Reference<EntityTypePosition>(3)))
                                    .ToXDocument(),
                        MessageType = (int)MessageTypeCode.FirmAssociatedPositionMustHavePrincipalWithDifferentBindingObject,
                        PeriodStart = MonthStart(1),
                        PeriodEnd = MonthStart(2),
                        OrderId = 0,
                    },

                    new Messages::Version.ValidationResult
                    {
                        MessageParams =
                                new MessageParams(
                                        new Reference<EntityTypeOrderPosition>(9,
                                            new Reference<EntityTypeOrder>(0),
                                            new Reference<EntityTypePosition>(2),
                                            new Reference<EntityTypePosition>(2)),

                                        new Reference<EntityTypeOrderPosition>(10,
                                            new Reference<EntityTypeOrder>(0),
                                            new Reference<EntityTypePosition>(3),
                                            new Reference<EntityTypePosition>(3)))
                                    .ToXDocument(),
                        MessageType = (int)MessageTypeCode.FirmAssociatedPositionMustHavePrincipalWithDifferentBindingObject,
                        PeriodStart = MonthStart(1),
                        PeriodEnd = MonthStart(2),
                        OrderId = 0,
                    });
    }
}
