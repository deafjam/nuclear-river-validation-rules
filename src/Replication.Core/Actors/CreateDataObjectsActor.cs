using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core.Commands;
using NuClear.Replication.Core.DataObjects;

namespace NuClear.Replication.Core.Actors
{
    public sealed class CreateDataObjectsActor<TDataObject> : IActor
        where TDataObject : class
    {
        private readonly IdentityChangesProvider<TDataObject> _changesProvider;
        private readonly IBulkRepository<TDataObject> _bulkRepository;
        private readonly IDataChangesHandler<TDataObject> _dataChangesHandler;

        public CreateDataObjectsActor(
            IdentityChangesProvider<TDataObject> changesProvider,
            IBulkRepository<TDataObject> bulkRepository,
            IDataChangesHandler<TDataObject> dataChangesHandler)
        {
            _changesProvider = changesProvider;
            _bulkRepository = bulkRepository;
            _dataChangesHandler = dataChangesHandler;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var commandsToExecute = commands.OfType<ICreateDataObjectCommand>()
                                            .Where(x => x.DataObjectType == typeof(TDataObject))
                                            .Distinct()
                                            .ToArray();

            if (!commandsToExecute.Any())
            {
                return Array.Empty<IEvent>();
            }

            var events = new List<IEvent>();

            var changes = _changesProvider.GetChanges(commandsToExecute);

            var toCreate = changes.Difference.ToArray();
            if (toCreate.Length != 0)
            {
                _bulkRepository.Create(toCreate);
                events.AddRange(_dataChangesHandler.HandleCreates(toCreate));
                events.AddRange(_dataChangesHandler.HandleRelates(toCreate));
            }

            return events;
        }
    }
}