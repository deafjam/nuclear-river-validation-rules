using System;
using System.Collections.Generic;
using NuClear.Replication.Core.Commands;

namespace NuClear.ValidationRules.Replication.Commands
{
    public abstract class AggregateCommand : IAggregateCommand
    {
        public Type AggregateRootType { get; }

        public IEnumerable<long> AggregateRootIds { get; }

        private AggregateCommand(Type aggregateRootType, IEnumerable<long> aggregateRootIds) =>
            (AggregateRootType, AggregateRootIds) = (aggregateRootType, aggregateRootIds);

        public sealed class Recalculate : AggregateCommand
        {
            public Recalculate(Type aggregateRootType, IEnumerable<long> aggregateRootIds) : base(aggregateRootType, aggregateRootIds) { }
        }
    }
}