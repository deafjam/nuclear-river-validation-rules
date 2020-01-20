using System;

namespace NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules
{
    public enum InvalidFirmState
    {
        NotSet = 0,
        Deleted,
        ClosedForever,
        ClosedForAscertainment,
        HasNoAddresses
    }
    
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

    public sealed class Order
    {
        public long Id { get; set; }
        public long FirmId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public long Scope { get; set; }

        public sealed class FirmOrganizationUnitMismatch
        {
            public long OrderId { get; set; }
        }

        public sealed class PartnerPosition
        {
            public long OrderId { get; set; }
            public long OrderPositionId { get; set; }
            public long DestinationFirmId { get; set; }
            public long DestinationFirmAddressId { get; set; }
        }

        public sealed class PremiumPartnerPosition
        {
            public long OrderId { get; set; }
        }

        public sealed class FmcgCutoutPosition
        {
            public long OrderId { get; set; }
        }

        public sealed class AddressAdvertisementNonOnTheMap
        {
            public long OrderId { get; set; }
            public long OrderPositionId { get; set; }
            public long PositionId { get; set; }
            public long AddressId { get; set; }
        }
        
        public sealed class MissingValidPartnerFirmAddresses
        {
            public long OrderId { get; set; }
            public long OrderPositionId { get; set; }
            public long PositionId { get; set; }
        }
        
        public sealed class InvalidFirm
        {
            public long OrderId { get; set; }
            public long FirmId { get; set; }
            public InvalidFirmState State { get; set; }
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
        
        public sealed class InvalidCategory
        {
            public long OrderId { get; set; }
            public long CategoryId { get; set; }
            public long OrderPositionId { get; set; }
            public long PositionId { get; set; }
            public InvalidCategoryState State { get; set; }
            public bool MayNotBelongToFirm { get; set; }
        }
        
        public sealed class CategoryNotBelongsToAddress
        {
            public long OrderId { get; set; }
            public long FirmAddressId { get; set; }
            public long CategoryId { get; set; }
            public long OrderPositionId { get; set; }
            public long PositionId { get; set; }
        }
    }
}