﻿using System;

namespace NuClear.ValidationRules.Storage.Model.ConsistencyRules.Facts
{
    public sealed class Order
    {
        public long Id { get; set; }
        public long FirmId { get; set; }
        public long DestOrganizationUnitId { get; set; }
        public long? LegalPersonId { get; set; }
        public long? LegalPersonProfileId { get; set; }
        public long? BranchOfficeOrganizationUnitId { get; set; }
        public long? InspectorId { get; set; }
        public long? CurrencyId { get; set; }
        public long? BargainId { get; set; }
        public long? DealId { get; set; }
        public int WorkflowStep { get; set; }
        public bool IsFreeOfCharge { get; set; }

        public string Number { get; set; }
        public DateTime SignupDate { get; set; }
        public DateTime BeginDistribution { get; set; }
        public DateTime EndDistributionFact { get; set; }
        public DateTime EndDistributionPlan { get; set; }
        public int ReleaseCountPlan { get; set; }
    }
}