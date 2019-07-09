using System.Collections.Generic;

using NuClear.Replication.Core;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.Events
{
    public sealed class ResultOutdatedEvent : IEvent
    {
        public MessageTypeCode Rule { get; }
        
        public ResultOutdatedEvent(MessageTypeCode rule) => Rule = rule;
    }
}