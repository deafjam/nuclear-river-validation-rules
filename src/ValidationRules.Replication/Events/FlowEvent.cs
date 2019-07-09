using NuClear.Messaging.API.Flows;
using NuClear.Replication.Core;

namespace NuClear.ValidationRules.Replication.Events
{
    public sealed class FlowEvent : IEvent
    {
        public IMessageFlow Flow { get; }
        public IEvent Event { get; }

        public FlowEvent(IMessageFlow flow, IEvent @event) =>
            (Flow, Event) = (flow, @event);
    }
}