using System;
using System.Collections.Generic;

using NuClear.Replication.Core;

namespace NuClear.ValidationRules.Replication.Events
{
    public sealed class DataObjectUpdatedEvent : IEvent
    {
        public Type DataObjectType { get; }
        public IEnumerable<long> DataObjectIds { get; }

        public DataObjectUpdatedEvent(Type dataObjectType, IEnumerable<long> dataObjectIds) =>
            (DataObjectType, DataObjectIds) = (dataObjectType, dataObjectIds);
    }
}