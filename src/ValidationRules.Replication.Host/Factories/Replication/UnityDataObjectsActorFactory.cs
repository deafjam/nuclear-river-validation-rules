using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Practices.Unity;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.Commands;
using NuClear.Replication.Core.DataObjects;

namespace NuClear.ValidationRules.Replication.Host.Factories.Replication
{
    public sealed class UnityDataObjectsActorFactory : IDataObjectsActorFactoryRefactored
    {
        private readonly Type _syncDataObjectsActorType = typeof(SyncDataObjectsActor<>);
        // ReSharper disable once RedundantNameQualifier
        private readonly Type _replaceDataObjectsActorType = typeof(ValidationRules.Replication.ReplaceDataObjectsActor<>);

        private readonly IUnityContainer _unityContainer;
        
        private readonly IReadOnlyCollection<Type> _syncDataObjectTypes;
        private readonly IReadOnlyCollection<Type> _replaceDataObjectTypes;

        public UnityDataObjectsActorFactory(IUnityContainer unityContainer, IDataObjectTypesProvider dataObjectTypesProvider)
        {
            _unityContainer = unityContainer;
            _syncDataObjectTypes = dataObjectTypesProvider.Get<ISyncDataObjectCommand>();
            _replaceDataObjectTypes = dataObjectTypesProvider.Get<IReplaceDataObjectCommand>();
        }

        public IReadOnlyCollection<IActor> Create(IReadOnlyCollection<Type> dataObjectTypes) =>
            dataObjectTypes.SelectMany(Create).ToList();

        private IEnumerable<IActor> Create(Type dataObjectType)
        {
            if (_syncDataObjectTypes.Contains(dataObjectType))
                yield return (IActor) _unityContainer.Resolve(_syncDataObjectsActorType.MakeGenericType(dataObjectType));
            else if (_replaceDataObjectTypes.Contains(dataObjectType))
                yield return (IActor) _unityContainer.Resolve( _replaceDataObjectsActorType.MakeGenericType(dataObjectType));
            else
                throw new ArgumentException($"Can't find data object actor for type {dataObjectType.GetFriendlyName()}");
        }
    }
}