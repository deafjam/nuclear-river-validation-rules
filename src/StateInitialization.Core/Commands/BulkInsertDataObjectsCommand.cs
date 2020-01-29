using System;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    internal sealed class BulkInsertDataObjectsCommand : ICommand
    {
        public BulkInsertDataObjectsCommand(
            TimeSpan bulkCopyTimeout)
        {
            BulkCopyTimeout = bulkCopyTimeout;
        }

        public TimeSpan BulkCopyTimeout { get; }
    }
}