using System.Collections.Generic;
using NuClear.Replication.Core;

namespace NuClear.ValidationRules.Replication.Events
{
    public sealed class PeriodKeysOutdatedEvent: IEvent
    {
        public IEnumerable<PeriodKey> PeriodKeys { get; }

        public PeriodKeysOutdatedEvent(IEnumerable<PeriodKey> periodKeys) => PeriodKeys = periodKeys;
    }
}