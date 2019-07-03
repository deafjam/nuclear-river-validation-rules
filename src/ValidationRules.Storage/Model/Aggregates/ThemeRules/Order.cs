using System;

namespace NuClear.ValidationRules.Storage.Model.Aggregates.ThemeRules
{
    public sealed class Order
    {
        public long Id { get; set; }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public long ProjectId { get; set; }

        public bool IsSelfAds { get; set; }

        public sealed class OrderTheme
        {
            public long OrderId { get; set; }
            public long ThemeId { get; set; }
        }
    }
}