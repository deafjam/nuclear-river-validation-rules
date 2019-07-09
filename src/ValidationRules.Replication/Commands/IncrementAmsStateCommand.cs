using NuClear.Replication.Core;

namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class IncrementAmsStateCommand : ICommand
    {
        public AmsState State { get; }
        
        public IncrementAmsStateCommand(AmsState state) => State = state;
    }
}