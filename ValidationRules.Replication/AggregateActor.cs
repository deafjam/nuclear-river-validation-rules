﻿using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.Commands;
using NuClear.Telemetry.Probing;
using NuClear.ValidationRules.Replication.Commands;

namespace NuClear.ValidationRules.Replication
{
    public class AggregateActor : IAggregateActor
    {
        private readonly LeafToRootActor _leafToRootActor;
        private readonly RootToLeafActor _rootToLeafActor;
        private readonly SubrootToLeafActor _subrootToLeafActor;
        private readonly IAggregateRootActor _aggregateRootActor;

        public AggregateActor(IAggregateRootActor aggregateRootActor)
        {
            _aggregateRootActor = aggregateRootActor;
            _leafToRootActor = new LeafToRootActor(aggregateRootActor);
            _rootToLeafActor = new RootToLeafActor(aggregateRootActor);
            _subrootToLeafActor = new SubrootToLeafActor(aggregateRootActor.GetEntityActors());
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var aggregateCommands =
                commands.OfType<IAggregateCommand>()
                        .Where(x => x.AggregateRootType == _aggregateRootActor.EntityType)
                        .Distinct()
                        .ToArray();

            var events = new List<IEvent>();

            if (!aggregateCommands.Any())
            {
                return events;
            }

            using (Probe.Create("Aggregate", _aggregateRootActor.EntityType.Name))
            {
                IReadOnlyCollection<ICommand> commandsToExecute =
                    aggregateCommands.OfType<DestroyAggregateCommand>()
                                     .SelectMany(next => new ICommand[]
                                                     {
                                                         new DeleteDataObjectCommand(next.AggregateRootType, next.AggregateRootId),
                                                         new ReplaceValueObjectCommand(next.AggregateRootId)
                                                     })
                                     .ToArray();
                events.AddRange(_leafToRootActor.ExecuteCommands(commandsToExecute));

                commandsToExecute =
                    aggregateCommands.OfType<InitializeAggregateCommand>()
                                     .SelectMany(next => new ICommand[]
                                                     {
                                                         new CreateDataObjectCommand(next.AggregateRootType, next.AggregateRootId),
                                                         new ReplaceValueObjectCommand(next.AggregateRootId)
                                                     })
                                     .ToArray();
                events.AddRange(_rootToLeafActor.ExecuteCommands(commandsToExecute));

                commandsToExecute =
                    aggregateCommands.OfType<RecalculateAggregateCommand>()
                                     .SelectMany(next => new ICommand[]
                                                     {
                                                         new SyncDataObjectCommand(next.AggregateRootType, next.AggregateRootId),
                                                         new ReplaceValueObjectCommand(next.AggregateRootId)
                                                     })
                                     .ToArray();
                events.AddRange(_rootToLeafActor.ExecuteCommands(commandsToExecute));

                commandsToExecute =
                    aggregateCommands.OfType<RecalculateEntityCommand>()
                                     .SelectMany(next => new ICommand[]
                                                     {
                                                         new SyncDataObjectCommand(next.EntityType, next.EntityId),
                                                         new ReplaceValueObjectCommand(next.AggregateRootId, next.EntityId)
                                                     })
                                     .ToArray();
                events.AddRange(_subrootToLeafActor.ExecuteCommands(commandsToExecute));

                commandsToExecute =
                    aggregateCommands.OfType<RecalculatePeriodAggregateCommand>()
                                     .SelectMany(next => new ICommand[]
                                                     {
                                                         new SyncPeriodDataObjectCommand(next.PeriodKey),
                                                         new ReplacePeriodValueObjectCommand(next.PeriodKey)
                                                     })
                                     .ToArray();
                events.AddRange(_subrootToLeafActor.ExecuteCommands(commandsToExecute));

                return events;
            }
        }
    }
}