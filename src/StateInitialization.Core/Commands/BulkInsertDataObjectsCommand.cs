using System;

using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    internal sealed class BulkInsertDataObjectsCommand : ICommand
    {
        public BulkInsertDataObjectsCommand(TimeSpan bulkCopyTimeout, TableName targetTable = null)
        {
            BulkCopyTimeout = bulkCopyTimeout;
            TargetTable = targetTable;
        }

        public TableName TargetTable { get; }

        public TimeSpan BulkCopyTimeout { get; }
    }
}
