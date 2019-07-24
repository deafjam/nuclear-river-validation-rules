using System;
using System.Collections.Generic;
using NuClear.Replication.Core.Commands;

namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class SyncPeriodCommand : ISyncDataObjectCommand
    {
        // TODO: DataObjectType не используется 
        public Type DataObjectType { get; }
        public IEnumerable<PeriodKey> PeriodKeys { get; }

        public SyncPeriodCommand(Type dataObjectType, IEnumerable<PeriodKey> periodKeys) =>
            (DataObjectType, PeriodKeys) = (dataObjectType, periodKeys);
    }
}