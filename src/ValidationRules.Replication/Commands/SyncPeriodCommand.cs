using System;
using System.Collections.Generic;
using NuClear.Replication.Core.Commands;

namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class SyncPeriodCommand : ISyncDataObjectCommand
    {
        public Type DataObjectType { get; }
        public IEnumerable<DateTime> Dates { get; }

        public SyncPeriodCommand(Type dataObjectType, IEnumerable<DateTime> dates) =>
            (DataObjectType, Dates) = (dataObjectType, dates);
    }
}