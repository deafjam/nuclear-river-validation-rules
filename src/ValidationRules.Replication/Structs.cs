using System;

namespace NuClear.ValidationRules.Replication
{
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