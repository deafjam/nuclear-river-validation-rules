using NuClear.Messaging.API.Flows;
using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.ValidationRules.StateInitialization.Host.Kafka
{
    internal sealed class KafkaReplicationCommand : ICommand
    {
        public KafkaReplicationCommand(IMessageFlow[] messageFlows, ReplicateInBulkCommand replicateInBulkCommand, int batchSize = 10000)
        {
            MessageFlows = messageFlows;
            ReplicateInBulkCommand = replicateInBulkCommand;
            BatchSize = batchSize;
        }

        public IMessageFlow[] MessageFlows { get; }
        public ReplicateInBulkCommand ReplicateInBulkCommand { get; }

        public int BatchSize { get; }
    }
}
