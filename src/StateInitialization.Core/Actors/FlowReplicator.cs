using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.DataObjects;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Factories;
using NuClear.Storage.API.Readings;

namespace NuClear.StateInitialization.Core.Actors
{
    internal sealed class FlowReplicator
    {
        private readonly IAccessorTypesProvider _accessorTypesProvider;

        public FlowReplicator(IAccessorTypesProvider accessorTypesProvider)
        {
            _accessorTypesProvider = accessorTypesProvider;
        }

        public void Replicate(IReadOnlyCollection<Type> dataObjectTypes, IEnumerable<ICommand> flow, DataConnection targetConnection, IReadOnlyCollection<ICommand> replicationCommands)
        {
            var actors = CreateActors(dataObjectTypes, flow, targetConnection);

            foreach (var actor in actors)
            {
                actor.ExecuteCommands(replicationCommands);
            }
        }

        private IEnumerable<IActor> CreateActors(
            IReadOnlyCollection<Type> dataObjectTypes,
            IEnumerable<ICommand> flow,
            DataConnection targetDataConnection)
        {
            var disableIndexesActorType = typeof(DisableIndexesActor);
            var disableIndexesActor = (IActor)Activator.CreateInstance(disableIndexesActorType, targetDataConnection.Connection);
            yield return disableIndexesActor;

            yield return new ProcessFlowActor(flow, CreateAccessors(dataObjectTypes, targetDataConnection).ToList());

            var enableIndexesActorType = typeof(EnableIndexesActor);
            var enableIndexesActor = (IActor)Activator.CreateInstance(enableIndexesActorType, targetDataConnection.Connection);
            yield return enableIndexesActor;

            var updateStatisticsActorType = typeof(UpdateTableStatisticsActor);
            var updateStatisticsActor = (IActor)Activator.CreateInstance(updateStatisticsActorType, targetDataConnection.Connection);
            yield return updateStatisticsActor;
        }

        private IEnumerable<AccessorDecorator> CreateAccessors(IReadOnlyCollection<Type> dataObjectTypes, DataConnection targetDataConnection)
        {
            foreach (var dataObjectType in dataObjectTypes)
            {
                var accessorTypes = _accessorTypesProvider.GetAccessorsFor(dataObjectType);
                foreach (var accessorType in accessorTypes)
                {
                    var memory = typeof(IMemoryBasedDataObjectAccessor<>).MakeGenericType(dataObjectType);
                    if (memory.IsAssignableFrom(accessorType))
                    {
                        yield return AccessorDecorator.Create(accessorType, dataObjectType, targetDataConnection);
                    }
                }
            }
        }
    }

    internal sealed class ProcessFlowActor : IActor
    {
        private readonly IEnumerable<ICommand> _flow;
        private readonly IReadOnlyCollection<AccessorDecorator> _accessorDecorators;

        public ProcessFlowActor(IEnumerable<ICommand> flow, IReadOnlyCollection<AccessorDecorator> accessorDecorators)
        {
            _flow = flow;
            _accessorDecorators = accessorDecorators;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var command = commands.OfType<BulkInsertDataObjectsCommand>().SingleOrDefault();
            if (command != null)
            {
                ExecuteBulkCopy();
            }

            return Array.Empty<IEvent>();
        }

        private void ExecuteBulkCopy()
        {
            foreach (var command in _flow)
            {
                foreach (var actor in _accessorDecorators)
                {
                    actor.ExecuteCommand(command);
                }
            }
        }
    }

    internal abstract class AccessorDecorator
    {
        public static AccessorDecorator Create(Type accessorType, Type dataObjectType, DataConnection targetDataConnection)
        {
            var accessor = Activator.CreateInstance(accessorType, (IQuery)null);
            return (AccessorDecorator)Activator.CreateInstance(typeof(AccessorDecoratorImpl<>).MakeGenericType(dataObjectType), accessor, targetDataConnection);
        }

        public abstract void ExecuteCommand(ICommand command);

        private sealed class AccessorDecoratorImpl<T> : AccessorDecorator
            where T : class
        {
            private readonly DataConnection _targetDataConnection;
            private readonly IMemoryBasedDataObjectAccessor<T> _accessor;

            public AccessorDecoratorImpl(IMemoryBasedDataObjectAccessor<T> accessor, DataConnection targetDataConnection)
            {
                _accessor = accessor;
                _targetDataConnection = targetDataConnection;
            }

            public override void ExecuteCommand(ICommand command)
            {
                var dataObjects = _accessor.GetDataObjects(command);
                if (dataObjects.Count != 0)
                {
                    var temp = _targetDataConnection.CreateTable<T>("#temp");
                    temp.BulkCopy(dataObjects);

                    var target = _targetDataConnection.GetTable<T>();
                    target.Delete(x => temp.Contains(x)); // linq2db, используя схему, сам построит удаление по primary key
                    temp.Insert(target, x => x);

                    temp.Drop();
                }
            }
        }
    }
}