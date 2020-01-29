using System;
using System.Collections.Generic;

using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Commands
{
    [Flags]
    public enum DbManagementMode
    {
        None = 0,
        DropAndRecreateViews = 1,
        DropAndRecreateConstraints = 2,
        EnableIndexManagment = 4,
        UpdateTableStatistics = 8,
        TruncateTable = 16,
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class ReplicateInBulkCommand : ICommand
    {
        private static readonly TimeSpan DefaultBulkCopyTimeout = TimeSpan.FromMinutes(30);

        public ReplicateInBulkCommand(
            IReadOnlyCollection<Type> typesToReplicate,
            StorageDescriptor sourceStorageDescriptor,
            StorageDescriptor targetStorageDescriptor,
            DbManagementMode databaseManagementMode = DbManagementMode.DropAndRecreateConstraints | DbManagementMode.EnableIndexManagment | DbManagementMode.UpdateTableStatistics | DbManagementMode.TruncateTable,
            ExecutionMode executionMode = null,
            TimeSpan? bulkCopyTimeout = null)
        {
            TypesToReplicate = typesToReplicate;
            SourceStorageDescriptor = sourceStorageDescriptor;
            TargetStorageDescriptor = targetStorageDescriptor;
            DbManagementMode = databaseManagementMode;
            ExecutionMode = executionMode ?? ExecutionMode.Parallel;
            BulkCopyTimeout = bulkCopyTimeout ?? DefaultBulkCopyTimeout;
        }

        public IReadOnlyCollection<Type> TypesToReplicate { get; }
        public StorageDescriptor SourceStorageDescriptor { get; }
        public StorageDescriptor TargetStorageDescriptor { get; }
        public DbManagementMode DbManagementMode { get; }
        public ExecutionMode ExecutionMode { get; }
        public TimeSpan BulkCopyTimeout { get; }
    }
}
