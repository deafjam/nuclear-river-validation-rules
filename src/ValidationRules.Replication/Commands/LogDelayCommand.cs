using System;

using NuClear.Replication.Core;

namespace NuClear.ValidationRules.Replication.Commands
{
    public sealed class LogDelayCommand : ICommand
    {
        public DateTime EventTime { get; }
        
        public LogDelayCommand(DateTime eventTime) => EventTime = eventTime;
    }
}
