using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Data;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Tenancy;
using NuClear.StateInitialization.Core.Factories;
using NuClear.StateInitialization.Core.Storage;
using NuClear.Storage.API.Specifications;

namespace NuClear.StateInitialization.Core.Actors
{
    internal sealed class BulkReplicator
    {
        private readonly IAccessorTypesProvider _accessorTypesProvider;

        public BulkReplicator(IAccessorTypesProvider accessorTypesProvider)
        {
            _accessorTypesProvider = accessorTypesProvider;
        }

        public void Replicate(
            IReadOnlyCollection<Type> dataObjectTypes,
            DataConnection sourceConnection,
            DataConnection targetConnection,
            IReadOnlyCollection<ICommand> replicationCommands,
            Tenant? tenant)
        {
            var actors = CreateActors(dataObjectTypes, sourceConnection, targetConnection, tenant);

            foreach (var actor in actors)
            {
                actor.ExecuteCommands(replicationCommands);
            }
        }

        private IReadOnlyCollection<IActor> CreateActors(
            IReadOnlyCollection<Type> dataObjectTypes,
            DataConnection sourceDataConnection,
            DataConnection targetDataConnection,
            Tenant? tenant)
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

            actors.AddRange(CreateBulkInsertDataObjectsActors(dataObjectTypes, sourceDataConnection, targetDataConnection, tenant));

            var enableIndexesActorType = typeof(EnableIndexesActor);
            var enableIndexesActor = (IActor)Activator.CreateInstance(enableIndexesActorType, targetDataConnection.Connection);
            actors.Add(enableIndexesActor);

            var updateStatisticsActorType = typeof(UpdateTableStatisticsActor);
            var updateStatisticsActor = (IActor)Activator.CreateInstance(updateStatisticsActorType, targetDataConnection.Connection);
            actors.Add(updateStatisticsActor);

            return actors;
        }

        private IEnumerable<IActor> CreateBulkInsertDataObjectsActors(
            IReadOnlyCollection<Type> dataObjectTypes,
            DataConnection sourceDataConnection,
            DataConnection targetDataConnection,
            Tenant? tenant)
        {
            foreach (var dataObjectType in dataObjectTypes)
            {
                var accessorTypes = _accessorTypesProvider.GetAccessorsFor(dataObjectType);
                foreach (var accessorType in accessorTypes)
                {
                    var storageAccessorType = typeof(IStorageBasedDataObjectAccessor<>).MakeGenericType(dataObjectType);
                    if (storageAccessorType.IsAssignableFrom(accessorType))
                    {
                        var query = new LinqToDbQuery(sourceDataConnection);
                        var accessorInstance = Activator.CreateInstance(accessorType, query);
                        if (tenant.HasValue)
                        {
                            var tenantAccessorType = typeof(TenantAccessor<>).MakeGenericType(dataObjectType);
                            accessorInstance = Activator.CreateInstance(tenantAccessorType, accessorInstance, tenant);
                        }
                        var replaceDataObjectsActorType = typeof(BulkInsertDataObjectsActor<>).MakeGenericType(dataObjectType);
                        var replaceDataObjectsActor = (IActor)Activator.CreateInstance(replaceDataObjectsActorType, accessorInstance, targetDataConnection);
                        yield return replaceDataObjectsActor;
                    }
                }
            }
        }

        private sealed class TenantAccessor<T> : IStorageBasedDataObjectAccessor<T>
        {
            private readonly IStorageBasedDataObjectAccessor<T> _implementation;
            private readonly Tenant _tenant;

            public TenantAccessor(IStorageBasedDataObjectAccessor<T> implementation, Tenant tenant)
            {
                _implementation = implementation;
                _tenant = tenant;
            }

            public IQueryable<T> GetSource()
                => typeof(ITenantEntity).IsAssignableFrom(typeof(T))
                    ? _implementation.GetSource().Select(_tenant.ApplyToEntity<T>())
                    : _implementation.GetSource();

            public FindSpecification<T> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
                => _implementation.GetFindSpecification(commands);
        }
    }
}