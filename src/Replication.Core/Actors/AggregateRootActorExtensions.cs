using System;
using System.Collections.Generic;
using System.Linq;

namespace NuClear.Replication.Core.Actors
{
    public static class AggregateRootActorExtensions
    {
        public  static IReadOnlyCollection<Type> GetEntityTypes(this IAggregateRootActor aggregateRootActor)
        {
            return aggregateRootActor.GetEntityActors().Select(x => x.EntityType).ToArray();
        }
    }
}