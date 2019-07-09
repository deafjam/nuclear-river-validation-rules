using System;
using System.Collections.Generic;
using System.Linq;
using NuClear.Replication.Core;
using NuClear.ValidationRules.Replication.Events;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.OperationsProcessing.AggregatesFlow
{
    internal sealed class AggregateEventCollector
    {
        private const int BatchSize = 100;

        private readonly HashSet<MessageTypeCode> _outdatedEvents = new HashSet<MessageTypeCode>();
        private readonly Dictionary<MessageTypeCode, HashSet<long>> _partiallyOutdatedEvents = new Dictionary<MessageTypeCode, HashSet<long>>();
        
        public void Add(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                Add(@event);
            }
        }

        private void Add(IEvent @event)
        {
            switch (@event)
            {
                case ResultOutdatedEvent outdatedEvent:
                {
                    _outdatedEvents.Add(outdatedEvent.Rule);
                    break;
                }

                case ResultPartiallyOutdatedEvent partiallyOutdatedEvent:
                {
                    if (!_partiallyOutdatedEvents.TryGetValue(partiallyOutdatedEvent.Rule, out var hashSet))
                    {
                        hashSet = new HashSet<long>();
                        _partiallyOutdatedEvents.Add(partiallyOutdatedEvent.Rule, hashSet);
                    }

                    hashSet.UnionWith(partiallyOutdatedEvent.OrderIds);
                    break;
                }
                    
                default:
                    throw new ArgumentOutOfRangeException(nameof(@event), $"Unexpected event type { @event.GetType().GetFriendlyName() }");
            }
        }

        public IEnumerable<IEvent> Events()
        {
            return _outdatedEvents.Select(x => new ResultOutdatedEvent(x)).Cast<IEvent>()
                .Concat(_partiallyOutdatedEvents.SelectMany(x => x.Value.CreateBatches(BatchSize).Select(y => new ResultPartiallyOutdatedEvent(x.Key, y))));
        }
    }
}