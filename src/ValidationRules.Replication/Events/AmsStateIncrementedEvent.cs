using NuClear.Replication.Core;

namespace NuClear.ValidationRules.Replication.Events
{
    public sealed class AmsStateIncrementedEvent : IEvent
    {
        public AmsState State { get; }
        public AmsStateIncrementedEvent(AmsState state) => State = state;
    }
}