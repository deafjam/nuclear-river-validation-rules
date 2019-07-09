namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class StoreAmsStateCommand : IValidationRuleCommand
    {
        public AmsState State { get; }
        
        public StoreAmsStateCommand(AmsState state) => State = state;
    }
}