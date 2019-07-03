using System;

namespace NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules
{
    public sealed class Firm
    {
        public long Id { get; set; }

        public class CategoryPurchase
        {
            public long FirmId { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public long Scope { get; set; }

            public long CategoryId { get; set; }
        }
    }
}