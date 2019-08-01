using System;

namespace NuClear.ValidationRules.Replication
{
    public struct PeriodKey
    {
        public long ProjectId { get; }
        public DateTime Date { get; }

        public PeriodKey(long projectId, DateTime date) =>
            (ProjectId, Date) = (projectId, date); 

        public bool Equals(PeriodKey other) => ProjectId == other.ProjectId && Date.Equals(other.Date);

        public override bool Equals(object obj) => obj is PeriodKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (ProjectId.GetHashCode() * 397) ^ Date.GetHashCode();
            }
        }
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