using System;
using System.Collections.Generic;
using NuClear.DataTest.Metamodel.Dsl;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.ConsistencyRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using MessageTypeCode = NuClear.ValidationRules.Storage.Model.Messages.MessageTypeCode;
using Version = NuClear.ValidationRules.Storage.Model.Messages.Version;

namespace NuClear.ValidationRules.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        private static ArrangeMetadataElement OrderRequiredFieldsShouldBeSpecified
            => ArrangeMetadataElement
                .Config
                .Name(nameof(OrderRequiredFieldsShouldBeSpecified))
                .Fact(
                    new Facts::Order { Id = 1, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(2)},
                    new Facts::OrderConsistency { Id = 1, LegalPersonId = null, LegalPersonProfileId = 3, BranchOfficeOrganizationUnitId = 4, HasCurrency = true, DealId = 0 },
                    
                    new Facts::Order { Id = 2, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(2)},
                    new Facts::OrderConsistency { Id = 2, LegalPersonId = 2, LegalPersonProfileId = null, BranchOfficeOrganizationUnitId = 4, HasCurrency = true, DealId = 0 },
                    
                    new Facts::Order { Id = 3, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(2)},
                    new Facts::OrderConsistency { Id = 3, LegalPersonId = 2, LegalPersonProfileId = 3, BranchOfficeOrganizationUnitId = null, HasCurrency = true, DealId = 0 },
                    
                    new Facts::Order { Id = 4, AgileDistributionStartDate = MonthStart(1), AgileDistributionEndPlanDate = MonthStart(2)},
                    new Facts::OrderConsistency { Id = 4, LegalPersonId = 2, LegalPersonProfileId = 3, BranchOfficeOrganizationUnitId = 4, HasCurrency = false, DealId = 0 })
                .Aggregate(
                    new Order { Id = 1, Start = MonthStart(1), End = MonthStart(2) },
                    CreateOrderMissingRequiredField(orderId: 1, legalPerson: true),
                    
                    new Order { Id = 2, Start = MonthStart(1), End = MonthStart(2) },
                    CreateOrderMissingRequiredField(orderId: 2, legalPersonProfile: true), 
                    
                    new Order { Id = 3, Start = MonthStart(1), End = MonthStart(2) },
                    CreateOrderMissingRequiredField(orderId: 3, branchOfficeOrganizationUnit: true),
                    
                    new Order { Id = 4, Start = MonthStart(1), End = MonthStart(2) },
                    CreateOrderMissingRequiredField(orderId: 4, currency: true))
                .Message(
                    CreateOrderRequiredFieldsShouldBeSpecified(1, MonthStart(1), MonthStart(2), legalPerson: true),
                    CreateOrderRequiredFieldsShouldBeSpecified(2, MonthStart(1), MonthStart(2), legalPersonProfile: true),
                    CreateOrderRequiredFieldsShouldBeSpecified(3, MonthStart(1), MonthStart(2), branchOfficeOrganizationUnit: true),
                    CreateOrderRequiredFieldsShouldBeSpecified(4, MonthStart(1), MonthStart(2), currency: true)
                    );

        private static Version.ValidationResult CreateOrderRequiredFieldsShouldBeSpecified(long orderId, DateTime start, DateTime end,
            bool legalPerson = false,
            bool legalPersonProfile = false,
            bool branchOfficeOrganizationUnit = false,
            bool currency = false) =>
            new Version.ValidationResult
            {
                MessageParams = new MessageParams(
                    new Dictionary<string, object>
                    {
                        { "legalPerson", legalPerson },
                        { "legalPersonProfile", legalPersonProfile },
                        { "branchOfficeOrganizationUnit", branchOfficeOrganizationUnit },
                        { "currency", currency },
                    },
                    new Reference<EntityTypeOrder>(orderId)).ToXDocument(),
                MessageType = (int)MessageTypeCode.OrderRequiredFieldsShouldBeSpecified,
                PeriodStart = start,
                PeriodEnd = end,
                OrderId = orderId,
            };

        private static Order.MissingRequiredField CreateOrderMissingRequiredField(long orderId,
            bool legalPerson = false,
            bool legalPersonProfile = false,
            bool branchOfficeOrganizationUnit = false,
            bool currency = false) =>
            new Order.MissingRequiredField
            {
                OrderId = orderId,
                LegalPerson = legalPerson,
                LegalPersonProfile = legalPersonProfile,
                BranchOfficeOrganizationUnit = branchOfficeOrganizationUnit,
                Currency = currency,
                Deal = false,
            };
    }
}