using System;
using System.Collections.Generic;

using NuClear.Replication.Core;

namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class IncrementErmStateCommand : ICommand
    {
        public IEnumerable<ErmState> States { get; }
        
        public IncrementErmStateCommand(IEnumerable<ErmState> states) => States = states;
    }
}