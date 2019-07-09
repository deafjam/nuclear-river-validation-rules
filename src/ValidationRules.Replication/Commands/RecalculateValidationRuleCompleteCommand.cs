using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class RecalculateValidationRuleCompleteCommand : IRecalculateValidationRuleCommand
    {
        public MessageTypeCode Rule { get; }
        
        public RecalculateValidationRuleCompleteCommand(MessageTypeCode rule) => Rule = rule;
    }
}