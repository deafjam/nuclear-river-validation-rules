using System;

namespace NuClear.ValidationRules.Storage.Model.Facts
{
    public sealed class CostPerClickCategoryRestriction
    {
        public long ProjectId { get; set; }
        public long CategoryId { get; set; }
        public DateTime Start { get; set; }
        public decimal MinCostPerClick { get; set; }
    }
}