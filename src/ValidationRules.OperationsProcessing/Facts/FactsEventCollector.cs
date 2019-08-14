using System;
using System.Collections.Generic;
using System.Linq;
using NuClear.Replication.Core;
using NuClear.ValidationRules.Replication;
using NuClear.ValidationRules.Replication.Events;

namespace NuClear.ValidationRules.OperationsProcessing.Facts
{
    internal sealed class FactsEventCollector
    {
        private const int BatchSize = 100;

        private readonly Dictionary<Type, HashSet<long>> _createdEvents = new Dictionary<Type, HashSet<long>>();
        private readonly Dictionary<Type, HashSet<long>> _updatedEvents = new Dictionary<Type, HashSet<long>>();
        private readonly Dictionary<Type, HashSet<long>> _deletedEvents = new Dictionary<Type, HashSet<long>>();
        private readonly Dictionary<(Type, Type), HashSet<long>> _relatedEvents = new Dictionary<(Type, Type), HashSet<long>>();

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
                case DataObjectCreatedEvent createdEvent:
                {
                    if (!_createdEvents.TryGetValue(createdEvent.DataObjectType, out var hashSet))
                    {
                        hashSet = new HashSet<long>();
                        _createdEvents.Add(createdEvent.DataObjectType, hashSet);
                    }

                    hashSet.UnionWith(createdEvent.DataObjectIds);
                    break;
                }

                case DataObjectUpdatedEvent updatedEvent:
                {
                    if (!_updatedEvents.TryGetValue(updatedEvent.DataObjectType, out var hashSet))
                    {
                        hashSet = new HashSet<long>();
                        _updatedEvents.Add(updatedEvent.DataObjectType, hashSet);
                    }

                    hashSet.UnionWith(updatedEvent.DataObjectIds);
                    break;
                }

                case DataObjectDeletedEvent deletedEvent:
                {
                    if (!_deletedEvents.TryGetValue(deletedEvent.DataObjectType, out var hashSet))
                    {
                        hashSet = new HashSet<long>();
                        _deletedEvents.Add(deletedEvent.DataObjectType, hashSet);
                    }

                    hashSet.UnionWith(deletedEvent.DataObjectIds);
                    break;
                }

                case RelatedDataObjectOutdatedEvent relatedEvent:
                {
                    var key = (relatedEvent.DataObjectType, relatedEvent.RelatedDataObjectType);
                    if (!_relatedEvents.TryGetValue(key, out var hashSet))
                    {
                        hashSet = new HashSet<long>();
                        _relatedEvents.Add(key, hashSet);
                    }

                    hashSet.UnionWith(relatedEvent.RelatedDataObjectIds);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(@event), $"Unexpected event type { @event.GetType().GetFriendlyName() }");
            }
        }

        public IEnumerable<IEvent> Events() =>
            _createdEvents
                .SelectMany(x => x.Value.CreateBatches(BatchSize).Select(y => new DataObjectCreatedEvent(x.Key, y))).Cast<IEvent>()
                .Concat(_updatedEvents.SelectMany(x => x.Value.CreateBatches(BatchSize).Select(y => new DataObjectUpdatedEvent(x.Key, y))))
                .Concat(_deletedEvents.SelectMany(x => x.Value.CreateBatches(BatchSize).Select(y => new DataObjectDeletedEvent(x.Key, y))))
                .Concat(_relatedEvents.SelectMany(x => x.Value.CreateBatches(BatchSize).Select(y => new RelatedDataObjectOutdatedEvent(x.Key.Item1, x.Key.Item2, y))));
    }
}