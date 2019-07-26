using System;
using System.Collections.Generic;
using NuClear.Replication.Core.Commands;
using NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;

namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class SyncPeriodCommand : ISyncDataObjectCommand
    {
        public Type DataObjectType => typeof(Period);
        public IEnumerable<PeriodKey> PeriodKeys { get; }

        public SyncPeriodCommand(IEnumerable<PeriodKey> periodKeys) =>
            PeriodKeys = periodKeys;
    }
}