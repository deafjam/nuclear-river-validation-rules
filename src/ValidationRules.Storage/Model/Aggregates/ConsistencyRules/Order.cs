using System;

namespace NuClear.ValidationRules.Storage.Model.Aggregates.ConsistencyRules
{
    public enum InvalidFirmAddressState
    {
        NotSet = 0,
        Deleted,
        NotActive,
        ClosedForAscertainment,
        NotBelongToFirm,
        MissingEntrance,
        InvalidBuildingPurpose
    }

    public enum InvalidCategoryState
    {
        NotSet = 0,
        Inactive,
        NotBelongToFirm
    }

    public enum DealState
    {
        NotSet = 0,
        Missing,
        Inactive
    }

    public sealed class Order
    {
        public long Id { get; set; }
        public DateTime BeginDistribution { get; set; }
        public DateTime EndDistributionFact { get; set; }
        public DateTime EndDistributionPlan { get; set; }

        public sealed class InactiveReference
        {
            public long OrderId { get; set; }
            public bool Deal { get; set; }
            public bool LegalPerson { get; set; }
            public bool LegalPersonProfile { get; set; }
            public bool BranchOfficeOrganizationUnit { get; set; }
            public bool BranchOffice { get; set; }
        }

        public sealed class InvalidFirmAddress
        {
            public long OrderId { get; set; }
            public long FirmAddressId { get; set; }
            public long OrderPositionId { get; set; }
            public long PositionId { get; set; }
            public InvalidFirmAddressState State { get; set; }
            public bool IsPartnerAddress { get; set; }
        }

        public sealed class CategoryNotBelongsToAddress
        {
            public long OrderId { get; set; }
            public long FirmAddressId { get; set; }
            public long CategoryId { get; set; }
            public long OrderPositionId { get; set; }
            public long PositionId { get; set; }
        }

        public sealed class InvalidCategory
        {
            public long OrderId { get; set; }
            public long CategoryId { get; set; }
            public long OrderPositionId { get; set; }
            public long PositionId { get; set; }
            public InvalidCategoryState State { get; set; }
            public bool MayNotBelongToFirm { get; set; }
        }

        public sealed class InvalidBeginDistributionDate
        {
            public long OrderId { get; set; }
        }

        public sealed class InvalidEndDistributionDate
        {
            public long OrderId { get; set; }
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

        public sealed class MissingValidPartnerFirmAddresses
        {
            public long OrderId { get; set; }
            public long OrderPositionId { get; set; }
            public long PositionId { get; set; }
        }
    }
}
