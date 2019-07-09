using System;
using System.Collections.Generic;
using NuClear.Replication.Core.Commands;

namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class SyncDataObjectCommand : ISyncDataObjectCommand
    {
        public Type DataObjectType { get; }
        public IEnumerable<long> DataObjectIds { get; }

        public SyncDataObjectCommand(Type dataObjectType, IEnumerable<long> ids) =>
            (DataObjectType, DataObjectIds) = (dataObjectType, ids);
    }
}