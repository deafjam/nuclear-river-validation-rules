using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class DisableIndexesCommand : ICommand
    {
        public DisableIndexesCommand(TableName table)
        {
            Table = table;
        }

        public TableName Table { get; }
    }
}