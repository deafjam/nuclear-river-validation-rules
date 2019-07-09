using System;

namespace NuClear.ValidationRules.Replication
{
    public struct PeriodKey
    {
        public DateTime Date { get; }

        public PeriodKey(DateTime date) => Date = date; 
        
        public bool Equals(PeriodKey other) => Date.Equals(other.Date);
        public override bool Equals(object obj) => obj is PeriodKey other && Equals(other);
        public override int GetHashCode() => Date.GetHashCode();
    }

    public struct ErmState
    {
        public Guid Token { get; }
        public DateTime UtcDateTime { get; }

        public ErmState(Guid token, DateTime utcDateTime) =>
            (Token, UtcDateTime) = (token, utcDateTime);
    }

    public struct AmsState
    {
        public long Offset { get; }
        public DateTime UtcDateTime { get; }

        public AmsState(long offset, DateTime utcDateTime) =>
            (Offset, UtcDateTime) = (offset, utcDateTime);
    }
}