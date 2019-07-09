using System;
using System.Collections.Generic;

using NuClear.Replication.Core;

namespace NuClear.ValidationRules.Replication.Events
{
    public sealed class DataObjectCreatedEvent : IEvent
    {
        public Type DataObjectType { get; }
        public IEnumerable<long> DataObjectIds { get; }

        public DataObjectCreatedEvent(Type dataObjectType, IEnumerable<long> dataObjectIds) => 
            (DataObjectType, DataObjectIds) = (dataObjectType, dataObjectIds);
    }
}