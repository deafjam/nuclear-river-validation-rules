using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class EnableIndexesCommand : ICommand
    {
        public EnableIndexesCommand(TableName table)
        {
            Table = table;
        }

        public TableName Table { get; }
    }
}