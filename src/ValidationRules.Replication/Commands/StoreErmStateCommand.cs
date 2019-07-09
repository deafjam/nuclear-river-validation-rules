using System.Collections.Generic;

namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class StoreErmStateCommand : IValidationRuleCommand
    {
        public IEnumerable<ErmState> States { get; }
        
        public StoreErmStateCommand(IEnumerable<ErmState> states) => States = states;
    }
}
