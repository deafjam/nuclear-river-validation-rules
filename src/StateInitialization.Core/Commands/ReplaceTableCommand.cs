using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class ReplaceTableCommand : ICommand
    {
        public ReplaceTableCommand(TableName tableToReplace, TableName replacementTable)
        {
            TableToReplace = tableToReplace;
            ReplacementTable = replacementTable;
        }

        public TableName TableToReplace { get; }

        public TableName ReplacementTable { get; }
    }
}
