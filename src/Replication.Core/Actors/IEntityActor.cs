using System;
using System.Collections.Generic;

namespace NuClear.Replication.Core.Actors
{
    public interface IEntityActor : IActor
    {
        Type EntityType { get; }
        IReadOnlyCollection<IActor> GetValueObjectActors();
    }
}