using System;

namespace NuClear.ValidationRules.Storage.Model.Facts
{
    public sealed class Order
    {
        public long Id { get; set; }
        public long FirmId { get; set; }
        public long ProjectId { get; set; }

        public DateTime AgileDistributionStartDate { get; set; }
        public DateTime AgileDistributionEndPlanDate { get; set; }
        public DateTime AgileDistributionEndFactDate { get; set; }

        public bool IsSelfAds { get; set; }
        public bool IsSelfSale { get; set; }
    }
}
