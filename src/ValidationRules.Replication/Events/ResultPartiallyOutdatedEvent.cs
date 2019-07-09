using System.Collections.Generic;

using NuClear.Replication.Core;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.Events
{
    public sealed class ResultPartiallyOutdatedEvent : IEvent
    {
        public MessageTypeCode Rule { get; }
        public IReadOnlyCollection<long> OrderIds { get; }

        public ResultPartiallyOutdatedEvent(MessageTypeCode rule, IReadOnlyCollection<long> orderIds) =>
            (Rule, OrderIds) = (rule, orderIds);
    }
}