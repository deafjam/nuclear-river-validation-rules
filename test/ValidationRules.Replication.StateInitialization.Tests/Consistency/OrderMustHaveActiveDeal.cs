using System.Collections.Generic;

using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.ConsistencyRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using Messages = NuClear.ValidationRules.Storage.Model.Messages;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement OrderMustHaveActiveDealAggregate
            => ArrangeMetadataElement
                .Config
                .Name(nameof(OrderMustHaveActiveDealAggregate))
                .Fact(
                    new Facts::Order { Id = 1, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(2)},
                    new Facts::OrderConsistency { Id = 1, DealId = null, HasCurrency = true },
                    new Facts::Order { Id = 2, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(2)},
                    new Facts::OrderConsistency { Id = 2, DealId = 2, HasCurrency = true },
                    new Facts::Order { Id = 3, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(2)},
                    new Facts::OrderConsistency { Id = 3, DealId = 3, HasCurrency = true },

                    new Facts::Deal { Id = 3 })
                .Aggregate(
                    new Order { Id = 1, Start = MonthStart(1), End = MonthStart(2) },
                    CreateOrderMissingRequiredField(orderId: 1, deal: true),
                    new Order.InactiveReference { OrderId = 1, Deal = false },

                    new Order { Id = 2, Start = MonthStart(1), End = MonthStart(2) },
                    CreateOrderMissingRequiredField(orderId: 2, deal: false),
                    new Order.InactiveReference { OrderId = 2, Deal = true },

                    new Order { Id = 3, Start = MonthStart(1), End = MonthStart(2) },
                    CreateOrderMissingRequiredField(orderId: 3, deal: false),
                    new Order.InactiveReference { OrderId = 3, Deal = false });

        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement OrderMustHaveActiveDealMessage
            => ArrangeMetadataElement
                .Config
                .Name(nameof(OrderMustHaveActiveDealMessage))
                .Aggregate(
                    new Order { Id = 1, Start = MonthStart(1), End = MonthStart(2) },
                    new Order.MissingRequiredField { OrderId = 1, Deal = true },
                    new Order.InactiveReference { OrderId = 1, Deal = false },

                    new Order { Id = 2, Start = MonthStart(1), End = MonthStart(2) },
                    new Order.MissingRequiredField { OrderId = 2, Deal = false },
                    new Order.InactiveReference { OrderId = 2, Deal = true },

                    new Order { Id = 3, Start = MonthStart(1), End = MonthStart(2) },
                    new Order.MissingRequiredField { OrderId = 3, Deal = false },
                    new Order.InactiveReference { OrderId = 3, Deal = false })
                .Message(
                    new Messages::Version.ValidationResult
                    {
                        MessageParams = new MessageParams(
                                new Dictionary<string, object> { { "state", (int)DealState.Missing } },
                                new Reference<EntityTypeOrder>(1)).ToXDocument(),
                        MessageType = (int)MessageTypeCode.OrderMustHaveActiveDeal,
                        PeriodStart = MonthStart(1),
                        PeriodEnd = MonthStart(2),
                        OrderId = 1,
                    },
                    new Messages::Version.ValidationResult
                    {
                        MessageParams = new MessageParams(
                                new Dictionary<string, object> { { "state", (int)DealState.Inactive } },
                                new Reference<EntityTypeOrder>(2)).ToXDocument(),
                        MessageType = (int)MessageTypeCode.OrderMustHaveActiveDeal,
                        PeriodStart = MonthStart(1),
                        PeriodEnd = MonthStart(2),
                        OrderId = 2,
                    });

        private static Order.MissingRequiredField CreateOrderMissingRequiredField(long orderId,
                                                                                      bool branchOfficeOrganizationUnit = true,
                                                                                      bool currency = false,
                                                                                      bool deal = true,
                                                                                      bool legalPerson = true,
                                                                                      bool legalPersonProfile = true)
        {
            return new Order.MissingRequiredField
                {
                    OrderId = orderId,
                    BranchOfficeOrganizationUnit = branchOfficeOrganizationUnit,
                    Currency = currency,
                    Deal = deal,
                    LegalPerson = legalPerson,
                    LegalPersonProfile = legalPersonProfile,
                };
        }
    }
}
