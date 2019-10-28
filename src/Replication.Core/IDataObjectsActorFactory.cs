using System;
using System.Collections.Generic;

using NuClear.Replication.Core.Actors;

namespace NuClear.Replication.Core
{
    public interface IDataObjectsActorFactory
    {
        IReadOnlyCollection<IActor> Create(IReadOnlyCollection<Type> dataObjectTypes);
    }
}