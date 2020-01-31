using System;
using System.Collections.Generic;
using System.Linq;
using NuClear.Replication.Core.Tenancy;
using NuClear.River.Hosting.Common.Identities.Connections;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Storage;
using NuClear.ValidationRules.Hosting.Common.Identities.Connections;
using NuClear.ValidationRules.Storage;

namespace NuClear.ValidationRules.StateInitialization.Host
{
    public static class BulkReplicationCommands
    {
        private static readonly ExecutionMode ParallelReplication = new ExecutionMode(4, false);

        public static ReplicateInBulkCommand AggregatesToMessages { get; } =
            ReplicateFromDbToDbCommand(
                DataObjectTypesProvider.MessagesTypes,
                new StorageDescriptor(ValidationRulesConnectionStringIdentity.Instance, Schema.Aggregates),
                new StorageDescriptor(ValidationRulesConnectionStringIdentity.Instance, Schema.Messages));

        public static ReplicateInBulkCommand[] ErmToMessages { get; } =
            Enum.GetValues(typeof(Tenant)).Cast<Tenant>().Select(tenant =>
                ReplicateFromDbToDbCommand(
                    DataObjectTypesProvider.ErmMessagesTypes,
                    new StorageDescriptor(ErmConnectionStringIdentity.Instance, Schema.Erm, tenant),
                    new StorageDescriptor(ValidationRulesConnectionStringIdentity.Instance, Schema.Messages)))
                .ToArray();

        public static ReplicateInBulkCommand FactsToAggregates { get; } =
            ReplicateFromDbToDbCommand(
                DataObjectTypesProvider.AggregateTypes,
                new StorageDescriptor(ValidationRulesConnectionStringIdentity.Instance, Schema.Facts),
                new StorageDescriptor(ValidationRulesConnectionStringIdentity.Instance, Schema.Aggregates));

        public static ReplicateInBulkCommand[] ErmToFacts { get; } =
            Enum.GetValues(typeof(Tenant)).Cast<Tenant>().Select(tenant =>
                    ReplicateFromDbToDbCommand(
                        DataObjectTypesProvider.ErmFactTypes,
                        new StorageDescriptor(ErmConnectionStringIdentity.Instance, Schema.Erm, tenant),
                        new StorageDescriptor(ValidationRulesConnectionStringIdentity.Instance, Schema.Facts)))
                .ToArray();

        public static ReplicateInBulkCommand KafkaToFacts { get; } =
            new ReplicateInBulkCommand(
                DataObjectTypesProvider.KafkaFactTypes,
                new StorageDescriptor(KafkaConnectionStringIdentity.Instance, null),
                new StorageDescriptor(ValidationRulesConnectionStringIdentity.Instance, Schema.Facts),
                DbManagementMode.UpdateTableStatistics);

        /// <summary>
        /// В databaseManagementMode исключен updatestatistics - причина, т.к. будет выполнен rebuild индексов, то
        /// статистика для индексов при этом будет автоматически пересчитана с FULLSCAN, нет смысла после этого делать updatestatistics
        /// с меньшим SampleRate потенциально ухудшая качество статистики
        /// </summary>
        private static ReplicateInBulkCommand ReplicateFromDbToDbCommand(
            IReadOnlyCollection<Type> typesToReplicate, StorageDescriptor from, StorageDescriptor to) =>
            new ReplicateInBulkCommand(
                typesToReplicate,
                from,
                to,
                executionMode: ParallelReplication,
                databaseManagementMode: DbManagementMode.DropAndRecreateConstraints |
                                        DbManagementMode.EnableIndexManagment);
    }
}