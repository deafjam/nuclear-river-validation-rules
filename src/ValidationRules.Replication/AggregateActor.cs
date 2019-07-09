using System.Collections.Generic;
using System.Linq;

using LinqToDB.Common;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.Commands;
using NuClear.Telemetry.Probing;
using NuClear.ValidationRules.Replication.Commands;

namespace NuClear.ValidationRules.Replication
{
    public class AggregateActor : IAggregateActor
    {
        private readonly RootToLeafActor _rootToLeafActor;
        private readonly IAggregateRootActor _aggregateRootActor;

        public AggregateActor(IAggregateRootActor aggregateRootActor)
        {
            _aggregateRootActor = aggregateRootActor;
            _rootToLeafActor = new RootToLeafActor(aggregateRootActor);
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var aggregateCommands =
                commands.OfType<IAggregateCommand>()
                        .Where(x => x.AggregateRootType == _aggregateRootActor.EntityType)
                        .ToList();
            if (aggregateCommands.Count == 0)
            {
                return Array<IEvent>.Empty;
            }
            
            var aggregateNameParts = _aggregateRootActor.EntityType.FullName.Split('.').Reverse().ToList();
            using (Probe.Create("Aggregate", aggregateNameParts[2], aggregateNameParts[0]))
            {
                var events = new List<IEvent>();
                
                var recalculateCommands =
                    aggregateCommands.OfType<AggregateCommand.Recalculate>()
                                     .SelectMany(next => new ICommand[]
                                                     {
                                                         new SyncDataObjectCommand(next.AggregateRootType, next.AggregateRootIds),
                                                         new ReplaceValueObjectCommand(next.AggregateRootIds)
                                                     })
                                     .ToList();
                events.AddRange(_rootToLeafActor.ExecuteCommands(recalculateCommands));

                var recalculatePeriodCommands =
                    aggregateCommands.OfType<RecalculatePeriodCommand>()
                                     .Select(next => new SyncPeriodCommand(next.AggregateRootType, next.PeriodKeys.Select(x => x.Date)))
                                     .ToList();
                events.AddRange(_rootToLeafActor.ExecuteCommands(recalculatePeriodCommands));

                // TODO: вопрос: надо ли тут схлапывать events по distinct или не надо, проверить!!! 
                
                return events;
            }
        }
    }
}