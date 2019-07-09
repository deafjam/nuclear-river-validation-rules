using System;
using System.Collections.Generic;

using NuClear.Replication.Core;

namespace NuClear.ValidationRules.Replication.Events
{
    public sealed class DataObjectDeletedEvent : IEvent
    {
        public Type DataObjectType { get; }
        public IEnumerable<long> DataObjectIds { get; }

        public DataObjectDeletedEvent(Type dataObjectType, IEnumerable<long> dataObjectIds) =>
            (DataObjectType, DataObjectIds) = (dataObjectType, dataObjectIds);
    }
}