using System.Collections.Generic;
using System.Linq;
using NuClear.OperationsTracking.API.UseCases;
using NuClear.Replication.Core;
using NuClear.ValidationRules.Replication;
using NuClear.ValidationRules.Replication.Commands;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.Erm
{
    internal sealed class ErmFactsCommandFactory : ICommandFactory<TrackedUseCase>
    {
        public IEnumerable<ICommand> CreateCommands(TrackedUseCase trackedUseCase) =>
            EntityTypeCommands(trackedUseCase).Concat(
            ErmStateCommands(trackedUseCase));

        private static IEnumerable<ICommand> EntityTypeCommands(TrackedUseCase trackedUseCase)
        {
            var changes = trackedUseCase.Operations.SelectMany(x => x.AffectedEntities.Changes);
            foreach (var change in changes)
            {
                var entityTypeId = change.Key.Id;
                if (!EntityTypeMap.TryGetErmFactTypes(entityTypeId, out var factTypes))
                {
                    continue;
                }

                var entityIds = change.Value.Keys;
                var commands = factTypes.Select(x => new SyncDataObjectCommand(x, entityIds));

                foreach (var command in commands)
                {
                    yield return command;
                }
            }
        }
            
        private static IEnumerable<ICommand> ErmStateCommands(TrackedUseCase trackedUseCase)
        {
            yield return new IncrementErmStateCommand(new[]
            {
                new ErmState(trackedUseCase.Id, trackedUseCase.Context.Finished.UtcDateTime)
            });
        }
    }
}