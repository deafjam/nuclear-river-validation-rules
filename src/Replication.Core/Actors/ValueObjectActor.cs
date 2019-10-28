using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core.Commands;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Telemetry.Probing;

namespace NuClear.Replication.Core.Actors
{
    public sealed class ValueObjectActor<TDataObject> : IActor where TDataObject : class
    {
        private readonly ValueObjectChangesProvider<TDataObject> _changesProvider;
        private readonly IBulkRepository<TDataObject> _bulkRepository;
        private readonly IDataChangesHandler<TDataObject> _dataChangesHandler;

        public ValueObjectActor(
            IQuery query,
            IBulkRepository<TDataObject> bulkRepository,
            IEqualityComparerFactory equalityComparerFactory,
            IStorageBasedDataObjectAccessor<TDataObject> storageBasedDataObjectAccessor)
            : this(new ValueObjectChangesProvider<TDataObject>(query, storageBasedDataObjectAccessor, equalityComparerFactory), bulkRepository, new NullDataChangesHandler<TDataObject>())
        {
        }

        public ValueObjectActor(ValueObjectChangesProvider<TDataObject> changesProvider, IBulkRepository<TDataObject> bulkRepository)
            : this(changesProvider, bulkRepository, new NullDataChangesHandler<TDataObject>())
        {
        }

        public ValueObjectActor(ValueObjectChangesProvider<TDataObject> changesProvider, IBulkRepository<TDataObject> bulkRepository, IDataChangesHandler<TDataObject> dataChangesHandler)
        {
            _changesProvider = changesProvider;
            _bulkRepository = bulkRepository;
            _dataChangesHandler = dataChangesHandler;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var commandsToExecute = commands.OfType<IReplaceValueObjectCommand>().Distinct().ToArray();
            if (!commandsToExecute.Any())
            {
                return Array.Empty<IEvent>();
            }

            using (Probe.Create("ValueObject", typeof(TDataObject).Name))
            {
                var events = new List<IEvent>();

                var changes = _changesProvider.GetChanges(commandsToExecute);

                var toDelete = changes.Complement.ToArray();
                if (toDelete.Length != 0)
                {
                    events.AddRange(_dataChangesHandler.HandleRelates(toDelete));
                    events.AddRange(_dataChangesHandler.HandleDeletes(toDelete));
                    _bulkRepository.Delete(toDelete);
                }

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
}