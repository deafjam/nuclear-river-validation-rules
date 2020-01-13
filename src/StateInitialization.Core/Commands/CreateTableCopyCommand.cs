using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    internal sealed class CreateTableCopyCommand : ICommand
    {
        public CreateTableCopyCommand(TableName source, TableName target)
        {
            SourceTable = source;
            TargetTable = target;
        }

        public TableName SourceTable { get; }

        public TableName TargetTable { get; }
    }
}
