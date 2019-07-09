using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.OperationsProcessing.Transports;
using NuClear.ValidationRules.Replication;
using NuClear.ValidationRules.Replication.Events;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.OperationsProcessing.Transports
{
    public sealed class XmlEventSerializer : IXmlEventSerializer
    {
        private const string EventType = "type";
        private const string DataObjectType = "type";
        private const string DataObjectId = "id";
        private const string RelatedDataObjectType = "relatedType";
        private const string RelatedDataObjectId = "relatedId";
        private const string State = "state";
        private const string EventHappenedTime = "time";
        private const string RuleCode = "rule";
        private const string OrderId = "orderId";
        
        private const string Date = "date";

        private static readonly IReadOnlyDictionary<string, Type> SimpleTypes =
            AppDomain.CurrentDomain.GetAssemblies()
                     .Where(x => x.FullName.Contains("ValidationRules"))
                     .SelectMany(x => x.ExportedTypes)
                     .Where(x => x.IsClass && !x.IsAbstract && !x.IsGenericType)
                     .ToDictionary(x => x.FullName, x => x);

        public IEvent Deserialize(XElement @event)
        {
            if (IsEventOfType(@event, typeof(DataObjectCreatedEvent)))
            {
                var dataObjectType = @event.Element(DataObjectType);
                var dataObjectIds = @event.Elements(DataObjectId);
                return new DataObjectCreatedEvent(ResolveDataObjectType(dataObjectType?.Value), dataObjectIds.Select(x => (long)x).ToList());
            }

            if (IsEventOfType(@event, typeof(DataObjectUpdatedEvent)))
            {
                var dataObjectType = @event.Element(DataObjectType);
                var dataObjectIds = @event.Elements(DataObjectId);
                return new DataObjectUpdatedEvent(ResolveDataObjectType(dataObjectType?.Value), dataObjectIds.Select(x => (long)x).ToList());
            }

            if (IsEventOfType(@event, typeof(DataObjectDeletedEvent)))
            {
                var dataObjectType = @event.Element(DataObjectType);
                var dataObjectIds = @event.Elements(DataObjectId);
                return new DataObjectDeletedEvent(ResolveDataObjectType(dataObjectType?.Value), dataObjectIds.Select(x => (long)x).ToList());
            }

            if (IsEventOfType(@event, typeof(RelatedDataObjectOutdatedEvent)))
            {
                var dataObjectType = @event.Element(DataObjectType);
                var relatedDataObjectType = @event.Element(RelatedDataObjectType);
                var relatedDataObjectIds = @event.Elements(RelatedDataObjectId);
                
                return new RelatedDataObjectOutdatedEvent(
                    ResolveDataObjectType(dataObjectType?.Value),
                    ResolveDataObjectType(relatedDataObjectType?.Value),
                    relatedDataObjectIds.Select(x => (long)x).ToList());
            }

            if (IsEventOfType(@event, typeof(PeriodKeyOutdatedEvent)))
            {
                var relatedDataObjectIds = @event.Elements(RelatedDataObjectId);
                return new PeriodKeyOutdatedEvent(relatedDataObjectIds.Select(x => new PeriodKey((DateTime)x)).ToList());
            }

            if (IsEventOfType(@event, typeof(AmsStateIncrementedEvent)))
            {
                var amsState = @event.Elements(State).Single();
                return new AmsStateIncrementedEvent(new AmsState((long)amsState, (DateTime)amsState.Attribute(Date)));
            }

            if (IsEventOfType(@event, typeof(ErmStateIncrementedEvent)))
            {
                var ermStates = @event.Elements(State);
                return new ErmStateIncrementedEvent(ermStates.Select(x => new ErmState((Guid)x, (DateTime)x.Attribute(Date))));
            }

            if (IsEventOfType(@event, typeof(DelayLoggedEvent)))
            {
                var time = @event.Element(EventHappenedTime);
                return new DelayLoggedEvent((DateTime)time);
            }

            if (IsEventOfType(@event, typeof(ResultPartiallyOutdatedEvent)))
            {
                var rule = @event.Element(RuleCode);
                var orderIds = @event.Elements(OrderId);
                return new ResultPartiallyOutdatedEvent((MessageTypeCode)(int)rule, orderIds.Select(x => (long)x).ToList());
            }

            if (IsEventOfType(@event, typeof(ResultOutdatedEvent)))
            {
                var rule = @event.Element(RuleCode);
                return new ResultOutdatedEvent((MessageTypeCode)(int)rule);
            }

            throw new ArgumentException($"Event is unknown or cannot be deserialized: {@event}", nameof(@event));
        }

        public XElement Serialize(IEvent @event)
        {
            switch (@event)
            {
                case FlowEvent flowEvent:
                    return Serialize(flowEvent.Event);

                case DataObjectCreatedEvent createdEvent:
                    return CreateRecord(createdEvent, new []
                    {
                        new XElement(DataObjectType, createdEvent.DataObjectType.FullName)
                    }.Concat(createdEvent.DataObjectIds.Select(x => new XElement(DataObjectId, x))).ToArray());

                case DataObjectUpdatedEvent updatedEvent:
                    return CreateRecord(updatedEvent, new []
                    {
                        new XElement(DataObjectType, updatedEvent.DataObjectType.FullName)
                    }.Concat(updatedEvent.DataObjectIds.Select(x => new XElement(DataObjectId, x))).ToArray());

                case DataObjectDeletedEvent deletedEvent:
                    return CreateRecord(deletedEvent, new []
                    {
                        new XElement(DataObjectType, deletedEvent.DataObjectType.FullName)
                    }.Concat(deletedEvent.DataObjectIds.Select(x => new XElement(DataObjectId, x))).ToArray());

                case RelatedDataObjectOutdatedEvent outdatedEvent:
                    return CreateRecord(outdatedEvent, new []
                    {
                        new XElement(DataObjectType, outdatedEvent.DataObjectType.FullName),
                        new XElement(RelatedDataObjectType, outdatedEvent.RelatedDataObjectType.FullName),
                    }.Concat(outdatedEvent.RelatedDataObjectIds.Select(x => new XElement(RelatedDataObjectId, x))).ToArray());

                case PeriodKeyOutdatedEvent periodKeyOutdatedEvent:
                    return CreateRecord(periodKeyOutdatedEvent, periodKeyOutdatedEvent.PeriodKeys.Select(x => new XElement(RelatedDataObjectId, x.Date)).ToArray());

                case AmsStateIncrementedEvent amsStateIncrementedEvent:
                    return CreateRecord(amsStateIncrementedEvent,
                                        new XElement(State, new XAttribute(Date, amsStateIncrementedEvent.State.UtcDateTime), amsStateIncrementedEvent.State.Offset));

                case ErmStateIncrementedEvent ermStateIncrementedEvent:
                    return CreateRecord(ermStateIncrementedEvent,
                                        ermStateIncrementedEvent.States.Select(x => new XElement(State,
                                                                                                 new XAttribute(Date, x.UtcDateTime), x.Token)).ToArray());

                case DelayLoggedEvent delayLoggedEvent:
                    return CreateRecord(delayLoggedEvent, new XElement(EventHappenedTime, delayLoggedEvent.EventTime));

                case ResultOutdatedEvent resultOutdatedEvent:
                    return CreateRecord(resultOutdatedEvent, new XElement(RuleCode, (int)resultOutdatedEvent.Rule));

                case ResultPartiallyOutdatedEvent resultPartiallyOutdatedEvent:
                    {
                        var orderIds = resultPartiallyOutdatedEvent.OrderIds.Select(x => new XElement(OrderId, x));
                        return CreateRecord(resultPartiallyOutdatedEvent, new[] { new XElement(RuleCode, (int)resultPartiallyOutdatedEvent.Rule) }.Concat(orderIds).ToArray());
                    }

                default:
                    throw new ArgumentException($"Unknown event type: {@event.GetType().Name}", nameof(@event));
            }
        }

        private static bool IsEventOfType(XElement @event, Type eventType) =>
            @event.Attribute(EventType)?.Value == eventType.GetFriendlyName();

        private static Type ResolveDataObjectType(string typeName) =>
            SimpleTypes.TryGetValue(typeName, out var type) ? type : null;

        private static XElement CreateRecord(IEvent @event, params XElement[] elements)
            => new XElement("event", new XAttribute(EventType, @event.GetType().GetFriendlyName()), elements);
    }
}