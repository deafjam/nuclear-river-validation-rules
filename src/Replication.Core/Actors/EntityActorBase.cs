using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Telemetry.Probing;

namespace NuClear.Replication.Core.Actors
{
    public abstract class EntityActorBase<TDataObject> : IEntityActor
        where TDataObject : class
    {
        private readonly CreateDataObjectsActor<TDataObject> _createDataObjectsActor;
        private readonly SyncDataObjectsActor<TDataObject> _syncDataObjectsActor;
        private readonly DeleteDataObjectsActor<TDataObject> _deleteDataObjectsActor;

        protected EntityActorBase(IQuery query,
                                  IBulkRepository<TDataObject> bulkRepository,
                                  IEqualityComparerFactory equalityComparerFactory,
                                  IStorageBasedDataObjectAccessor<TDataObject> storageBasedDataObjectAccessor)
            : this(query, bulkRepository, equalityComparerFactory, storageBasedDataObjectAccessor, new NullDataChangesHandler<TDataObject>())
        {
        }

        protected EntityActorBase(IQuery query,
                                  IBulkRepository<TDataObject> bulkRepository,
                                  IEqualityComparerFactory equalityComparerFactory,
                                  IStorageBasedDataObjectAccessor<TDataObject> storageBasedDataObjectAccessor,
                                  IDataChangesHandler<TDataObject> dataChangesHandler)
            : this(new CreateDataObjectsActor<TDataObject>(new IdentityChangesProvider<TDataObject>(query, storageBasedDataObjectAccessor, equalityComparerFactory), bulkRepository, dataChangesHandler),
                   new SyncDataObjectsActor<TDataObject>(new EntityChangesProvider<TDataObject>(query, storageBasedDataObjectAccessor, equalityComparerFactory), bulkRepository, dataChangesHandler),
                   new DeleteDataObjectsActor<TDataObject>(new IdentityChangesProvider<TDataObject>(query, storageBasedDataObjectAccessor, equalityComparerFactory), bulkRepository, dataChangesHandler))
        {
        }

        protected EntityActorBase(
            CreateDataObjectsActor<TDataObject> createDataObjectsActor,
            SyncDataObjectsActor<TDataObject> syncDataObjectsActor,
            DeleteDataObjectsActor<TDataObject> deleteDataObjectsActorr)
        {
            _createDataObjectsActor = createDataObjectsActor;
            _syncDataObjectsActor = syncDataObjectsActor;
            _deleteDataObjectsActor = deleteDataObjectsActorr;
        }

        public Type EntityType => typeof(TDataObject);

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            if (!commands.Any())
            {
                return Array.Empty<IEvent>();
            }

            using (Probe.Create("Entity", typeof(TDataObject).Name))
            {
                var events = new List<IEvent>();

                events.AddRange(_createDataObjectsActor.ExecuteCommands(commands));
                events.AddRange(_syncDataObjectsActor.ExecuteCommands(commands));
                events.AddRange(_deleteDataObjectsActor.ExecuteCommands(commands));

                return events;
            }
        }
        public abstract IReadOnlyCollection<IActor> GetValueObjectActors();
    }
}