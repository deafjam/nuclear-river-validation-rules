using System;

namespace NuClear.ValidationRules.Replication.Host.Temp
{
    public sealed class ResultRequest
    {
        public string UserAccount { get; set; }
        public long OrderId { get; set; }
        public DateTime PeriodStart { get; set; }
    }
}