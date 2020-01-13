using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class UpdateTableStatisticsCommand : ICommand
    {
        public UpdateTableStatisticsCommand(TableName table)
        {
            Table = table;
        }

        public TableName Table { get; }
    }
}