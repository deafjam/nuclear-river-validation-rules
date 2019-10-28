using System;
using System.Collections.Generic;

using LinqToDB.Data;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.DataObjects;
using NuClear.StateInitialization.Core.Factories;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Actors
{
    internal sealed class BulkReplicator
    {
        private readonly IAccessorTypesProvider _accessorTypesProvider;

        public BulkReplicator(IAccessorTypesProvider accessorTypesProvider)
        {
            _accessorTypesProvider = accessorTypesProvider;
        }

        public void Replicate(IReadOnlyCollection<Type> dataObjectTypes, DataConnection sourceConnection, DataConnection targetConnection, IReadOnlyCollection<ICommand> replicationCommands)
        {
            var actors = CreateActors(dataObjectTypes, sourceConnection, targetConnection);

            foreach (var actor in actors)
            {
                actor.ExecuteCommands(replicationCommands);
            }
        }

        private IReadOnlyCollection<IActor> CreateActors(
            IReadOnlyCollection<Type> dataObjectTypes,
            DataConnection sourceDataConnection,
            DataConnection targetDataConnection)
        {
            var actors = new List<IActor>();

            var createTableCopyActorType = typeof(CreateTableCopyActor);
            var createTableCopyActor = (IActor)Activator.CreateInstance(createTableCopyActorType, targetDataConnection.Connection);
            actors.Add(createTableCopyActor);

            var disableIndexesActorType = typeof(DisableIndexesActor);
            var disableIndexesActor = (IActor)Activator.CreateInstance(disableIndexesActorType, targetDataConnection.Connection);
            actors.Add(disableIndexesActor);

            var truncateTableActorType = typeof(TruncateTableActor);
            var truncateTableActor = (IActor)Activator.CreateInstance(truncateTableActorType, targetDataConnection);
            actors.Add(truncateTableActor);

            actors.AddRange(CreateBulkInsertDataObjectsActors(dataObjectTypes, sourceDataConnection, targetDataConnection));

            var enableIndexesActorType = typeof(EnableIndexesActor);
            var enableIndexesActor = (IActor)Activator.CreateInstance(enableIndexesActorType, targetDataConnection.Connection);
            actors.Add(enableIndexesActor);

            var updateStatisticsActorType = typeof(UpdateTableStatisticsActor);
            var updateStatisticsActor = (IActor)Activator.CreateInstance(updateStatisticsActorType, targetDataConnection.Connection);
            actors.Add(updateStatisticsActor);

            return actors;
        }

        private IEnumerable<IActor> CreateBulkInsertDataObjectsActors(IReadOnlyCollection<Type> dataObjectTypes, DataConnection sourceDataConnection, DataConnection targetDataConnection)
        {
            foreach (var dataObjectType in dataObjectTypes)
            {
                var accessorTypes = _accessorTypesProvider.GetAccessorsFor(dataObjectType);
                foreach (var accessorType in accessorTypes)
                {
                    var storageAccessorType = typeof(IStorageBasedDataObjectAccessor<>).MakeGenericType(dataObjectType);
                    if (storageAccessorType.IsAssignableFrom(accessorType))
                    {
                        var accessorInstance = Activator.CreateInstance(accessorType, new LinqToDbQuery(sourceDataConnection));
                        var replaceDataObjectsActorType = typeof(BulkInsertDataObjectsActor<>).MakeGenericType(dataObjectType);
                        var replaceDataObjectsActor = (IActor)Activator.CreateInstance(replaceDataObjectsActorType, accessorInstance, targetDataConnection);
                        yield return replaceDataObjectsActor;
                    }
                }
            }
        }
    }
}