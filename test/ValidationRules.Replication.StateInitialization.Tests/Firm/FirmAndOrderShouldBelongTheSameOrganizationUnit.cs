using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement FirmAndOrderShouldBelongTheSameOrganizationUnit
            => ArrangeMetadataElement
                .Config
                .Name(nameof(FirmAndOrderShouldBelongTheSameOrganizationUnit))
                .Fact(
                    new Facts::Firm { Id = 1, ProjectId = 1},
                    new Facts::Order { Id = 2, FirmId = 1, ProjectId = 2, AgileDistributionStartDate = FirstDayJan, AgileDistributionEndFactDate = FirstDayFeb },
                    new Facts::OrderWorkflow {Id = 2, Step = 5}
                    )
                .Aggregate(
                    new Firm { Id = 1, },
                    new Order { Id = 2, FirmId = 1, Start = FirstDayJan, End = FirstDayFeb },
                    new Order.FirmOrganizationUnitMismatch { OrderId = 2 })
                .Message(
                    new Version.ValidationResult
                        {
                            MessageParams = new MessageParams(
                                    new Reference<EntityTypeOrder>(2)).ToXDocument(),
                            MessageType = (int)MessageTypeCode.FirmAndOrderShouldBelongTheSameOrganizationUnit,
                            PeriodStart = FirstDayJan,
                            PeriodEnd = FirstDayFeb,
                            OrderId = 2,
                        });
    }
}

