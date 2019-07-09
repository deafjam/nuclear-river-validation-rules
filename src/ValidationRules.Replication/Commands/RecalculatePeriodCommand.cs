using System;
using System.Collections.Generic;
using NuClear.Replication.Core.Commands;
using NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;

namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class RecalculatePeriodCommand : IAggregateCommand
    {
        public Type AggregateRootType => typeof(Period);
        public IEnumerable<PeriodKey> PeriodKeys { get; }

        public RecalculatePeriodCommand(IEnumerable<PeriodKey> periodKeys) => PeriodKeys = periodKeys;
    }
}