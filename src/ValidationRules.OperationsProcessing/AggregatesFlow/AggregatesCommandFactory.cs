using System;
using System.Collections.Generic;
using System.Linq;
using NuClear.Replication.Core;
using NuClear.Replication.OperationsProcessing;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Events;

namespace NuClear.ValidationRules.OperationsProcessing.AggregatesFlow
{
    internal sealed class AggregatesCommandFactory : ICommandFactory<EventMessage>
    {
        public IEnumerable<ICommand> CreateCommands(EventMessage message)
        {
            switch (message.Event)
            {
                case DataObjectCreatedEvent createdEvent:
                    return AggregateTypesFor<DataObjectCreatedEvent>(createdEvent.DataObjectType)
                        .Select(x => new AggregateCommand.Recalculate(x, createdEvent.DataObjectIds));

                case DataObjectUpdatedEvent updatedEvent:
                    return AggregateTypesFor<DataObjectUpdatedEvent>(updatedEvent.DataObjectType)
                        .Select(x => new AggregateCommand.Recalculate(x, updatedEvent.DataObjectIds));

                case DataObjectDeletedEvent deletedEvent:
                    return AggregateTypesFor<DataObjectDeletedEvent>(deletedEvent.DataObjectType)
                        .Select(x => new AggregateCommand.Recalculate(x, deletedEvent.DataObjectIds));

                case RelatedDataObjectOutdatedEvent outdatedEvent:
                    return RelatedAggregateTypesFor<RelatedDataObjectOutdatedEvent>(outdatedEvent.DataObjectType, outdatedEvent.RelatedDataObjectType)
                        .Select(x => new AggregateCommand.Recalculate(x, outdatedEvent.RelatedDataObjectIds));

                case PeriodKeysOutdatedEvent periodKeysOutdatedEvent:
                    return Enumerable.Repeat(new RecalculatePeriodCommand(periodKeysOutdatedEvent.PeriodKeys), 1);

                case AmsStateIncrementedEvent amsStateIncrementedEvent:
                    return Enumerable.Repeat(new IncrementAmsStateCommand(amsStateIncrementedEvent.State), 1);

                case ErmStateIncrementedEvent ermStateIncrementedEvent:
                    return Enumerable.Repeat(new IncrementErmStateCommand(ermStateIncrementedEvent.States), 1);

                case DelayLoggedEvent delayLoggedEvent:
                    return Enumerable.Repeat(new LogDelayCommand(delayLoggedEvent.EventTime), 1);

                default:
                    throw new ArgumentException($"Unexpected event '{message.Event}'", nameof(message.Event));
            }
        }

        private static IEnumerable<Type> AggregateTypesFor<TEvent>(Type dataObjectType)
            where TEvent : IEvent
        {
            if (!EntityTypeMap.TryGetAggregateTypes(dataObjectType, out var aggregateTypes))
            {
                throw new ArgumentException(
                    $"No metadata for event {typeof(TEvent).Name}, DataObjectType={dataObjectType.Name}",
                    nameof(dataObjectType));
            }

            return aggregateTypes;
        }

        private static IEnumerable<Type> RelatedAggregateTypesFor<TEvent>(Type dataObjectType,
            Type relatedDataObjectType)
            where TEvent : IEvent
        {
            if (!EntityTypeMap.TryGetRelatedAggregateTypes(dataObjectType, relatedDataObjectType,
                out var aggregateTypes))
            {
                throw new ArgumentException(
                    $"No metadata for event {typeof(TEvent).GetFriendlyName()} ({dataObjectType.Name}, {relatedDataObjectType.Name})",
                    nameof(dataObjectType));
            }

            return aggregateTypes;
        }
    }
}