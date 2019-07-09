using System.Collections.Generic;
using NuClear.Replication.Core;

namespace NuClear.ValidationRules.Replication.Events
{
    public sealed class PeriodKeyOutdatedEvent: IEvent
    {
        public IEnumerable<PeriodKey> PeriodKeys { get; }

        public PeriodKeyOutdatedEvent(IEnumerable<PeriodKey> relatedPeriodKeys) => PeriodKeys = relatedPeriodKeys;
    }
}