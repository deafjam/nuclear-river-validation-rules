using System;
using System.Collections.Generic;

using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Storage;
using NuClear.Storage.API.ConnectionStrings;

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
            StorageDescriptor sourceStorageDescriptor,
            StorageDescriptor targetStorageDescriptor,
            DbManagementMode databaseManagementMode = DbManagementMode.DropAndRecreateConstraints | DbManagementMode.EnableIndexManagment | DbManagementMode.UpdateTableStatistics | DbManagementMode.TruncateTable,
            ExecutionMode executionMode = null,
            TimeSpan? bulkCopyTimeout = null)
        {
            SourceStorageDescriptor = sourceStorageDescriptor;
            TargetStorageDescriptor = targetStorageDescriptor;
            DbManagementMode = databaseManagementMode;
            ExecutionMode = executionMode ?? ExecutionMode.Parallel;
            BulkCopyTimeout = bulkCopyTimeout ?? DefaultBulkCopyTimeout;
        }

        public StorageDescriptor SourceStorageDescriptor { get; }
        public StorageDescriptor TargetStorageDescriptor { get; }
        public DbManagementMode DbManagementMode { get; }
        public ExecutionMode ExecutionMode { get; }
        public TimeSpan BulkCopyTimeout { get; }
    }

    public sealed class InitializeFromFlowCommand : ICommand
    {
        private static readonly TimeSpan DefaultBulkCopyTimeout = TimeSpan.FromMinutes(30);

        public InitializeFromFlowCommand(
            Func<IConnectionStringSettings, IEnumerable<ICommand>> flowFactory,
            StorageDescriptor targetStorageDescriptor,
            DbManagementMode databaseManagementMode = DbManagementMode.DropAndRecreateConstraints | DbManagementMode.EnableIndexManagment | DbManagementMode.UpdateTableStatistics,
            TimeSpan? bulkCopyTimeout = null)
        {
            TargetStorageDescriptor = targetStorageDescriptor;
            FlowFactory = flowFactory;
            DbManagementMode = databaseManagementMode;
            BulkCopyTimeout = bulkCopyTimeout ?? DefaultBulkCopyTimeout;
        }
        public StorageDescriptor TargetStorageDescriptor { get; set; }
        public DbManagementMode DbManagementMode { get; set; }
        public TimeSpan BulkCopyTimeout { get; }
        public Func<IConnectionStringSettings, IEnumerable<ICommand>> FlowFactory { get; set; }
    }
}
