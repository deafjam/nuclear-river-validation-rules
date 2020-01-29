using NuClear.Messaging.API.Flows;
using NuClear.Replication.Core;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.ValidationRules.StateInitialization.Host.Kafka
{
    internal sealed class KafkaReplicationCommand : ICommand
    {
        public KafkaReplicationCommand(IMessageFlow messageFlow, ReplicateInBulkCommand replicateInBulkCommand, int batchSize = 5000)
        {
            MessageFlow = messageFlow;
            ReplicateInBulkCommand = replicateInBulkCommand;
            BatchSize = batchSize;
        }

        public IMessageFlow MessageFlow { get; }
        public ReplicateInBulkCommand ReplicateInBulkCommand { get; }

        public int BatchSize { get; }
    }
}
