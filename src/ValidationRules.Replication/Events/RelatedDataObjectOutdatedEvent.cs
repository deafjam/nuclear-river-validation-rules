using System;
using System.Collections.Generic;

using NuClear.Replication.Core;

namespace NuClear.ValidationRules.Replication.Events
{
    public sealed class RelatedDataObjectOutdatedEvent : IEvent
    {
        public Type DataObjectType { get; }
        public Type RelatedDataObjectType { get; }
        
        // related ids не могут вычисляться лениво
        // т.к. их вычисление требует реального запроса в базу
        public IReadOnlyCollection<long> RelatedDataObjectIds { get; }

        public RelatedDataObjectOutdatedEvent(Type dataObjectType, Type relatedDataObjectType, IReadOnlyCollection<long> relatedDataObjectIds) =>
            (DataObjectType, RelatedDataObjectType, RelatedDataObjectIds) = (dataObjectType, relatedDataObjectType, relatedDataObjectIds);
    }
}