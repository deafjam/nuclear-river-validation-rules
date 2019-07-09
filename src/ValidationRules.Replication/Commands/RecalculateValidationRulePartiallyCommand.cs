using System.Collections.Generic;

using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class RecalculateValidationRulePartiallyCommand : IRecalculateValidationRuleCommand
    {
        public MessageTypeCode Rule { get; }
        public IReadOnlyCollection<long> Filter { get; }

        public RecalculateValidationRulePartiallyCommand(MessageTypeCode rule, IReadOnlyCollection<long> filter) =>
            (Rule, Filter) = (rule, filter);
    }
}