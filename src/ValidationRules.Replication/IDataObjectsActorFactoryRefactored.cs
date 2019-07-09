using System;
using System.Collections.Generic;
using NuClear.Replication.Core.Actors;

namespace NuClear.ValidationRules.Replication
{
    // TODO: перенести изменения по IDataObjectsActorFactory в базовый проект Replication.Core
    public interface IDataObjectsActorFactoryRefactored
    {
        IReadOnlyCollection<IActor> Create(IReadOnlyCollection<Type> dataObjectTypes);
    }
}