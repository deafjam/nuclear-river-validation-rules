using System;

namespace NuClear.ValidationRules.Storage.Model.Aggregates.ConsistencyRules
{
    public enum DealState
    {
        NotSet = 0,
        Missing,
        Inactive
    }

    public sealed class Order
    {
        public long Id { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public sealed class InactiveReference
        {
            public long OrderId { get; set; }
            public bool Deal { get; set; }
            public bool LegalPerson { get; set; }
            public bool LegalPersonProfile { get; set; }
            public bool BranchOfficeOrganizationUnit { get; set; }
            public bool BranchOffice { get; set; }
        }

        public sealed class LegalPersonProfileBargainExpired
        {
            public long OrderId { get; set; }
            public long LegalPersonProfileId { get; set; }
        }

        public sealed class LegalPersonProfileWarrantyExpired
        {
            public long OrderId { get; set; }
            public long LegalPersonProfileId { get; set; }
        }

        public sealed class BargainSignedLaterThanOrder
        {
            public long OrderId { get; set; }
        }

        public class MissingBargainScan
        {
            public long OrderId { get; set; }
        }

        public sealed class MissingOrderScan
        {
            public long OrderId { get; set; }
        }

        public sealed class HasNoAnyLegalPersonProfile
        {
            public long OrderId { get; set; }
        }

        public sealed class HasNoAnyPosition
        {
            public long OrderId { get; set; }
        }

        public sealed class MissingBills
        {
            public long OrderId { get; set; }
        }

        public sealed class InvalidBillsTotal
        {
            public long OrderId { get; set; }
        }

        public sealed class MissingRequiredField
        {
            public long OrderId { get; set; }
            public bool LegalPerson { get; set; }
            public bool LegalPersonProfile { get; set; }
            public bool BranchOfficeOrganizationUnit { get; set; }
            public bool Currency { get; set; }
            public bool Deal { get; set; }
        }
    }
}
