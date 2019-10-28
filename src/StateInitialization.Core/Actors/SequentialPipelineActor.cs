using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;

namespace NuClear.StateInitialization.Core.Actors
{
    internal sealed class SequentialPipelineActor : IActor
    {
        private readonly IReadOnlyCollection<IActor> _actors;

        public SequentialPipelineActor(IReadOnlyCollection<IActor> actors)
        {
            _actors = actors;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            if (!commands.Any())
            {
                return Array.Empty<IEvent>();
            }

            return _actors.Aggregate(
                new List<IEvent>(),
                (events, actor) =>
                    {
                        var sw = Stopwatch.StartNew();
                        events.AddRange(actor.ExecuteCommands(commands));
                        sw.Stop();
                        Console.WriteLine($"[{DateTime.Now}] [{Environment.CurrentManagedThreadId}] {actor.GetType().GetFriendlyName()}: {sw.Elapsed.TotalSeconds:F3} seconds");

                        return events;
                    });
        }
    }
}