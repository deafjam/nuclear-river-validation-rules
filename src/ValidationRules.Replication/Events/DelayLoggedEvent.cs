using System;

using NuClear.Replication.Core;

namespace NuClear.ValidationRules.Replication.Events
{
    public sealed class DelayLoggedEvent : IEvent
    {
        public DateTime EventTime { get; }

        public DelayLoggedEvent(DateTime eventTime) => EventTime = eventTime;  
    }
}