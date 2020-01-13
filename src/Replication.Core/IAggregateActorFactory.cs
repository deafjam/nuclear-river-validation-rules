using System;
using System.Collections.Generic;

using NuClear.Replication.Core.Actors;

namespace NuClear.Replication.Core
{
    public interface IAggregateActorFactory
    {
        IReadOnlyCollection<IActor> Create(IReadOnlyCollection<Type> aggregateRootTypes);
    }
}