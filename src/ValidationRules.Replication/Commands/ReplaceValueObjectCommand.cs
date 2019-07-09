using System.Collections.Generic;
using NuClear.Replication.Core.Commands;

namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class ReplaceValueObjectCommand : IReplaceValueObjectCommand
    {
        public IEnumerable<long> AggregateRootIds { get; }

        public ReplaceValueObjectCommand(IEnumerable<long> aggregateRootIds) => AggregateRootIds = aggregateRootIds;
    }
}