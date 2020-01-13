using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using NuClear.Messaging.API;
using NuClear.Messaging.API.Flows.Metadata;
using NuClear.Messaging.API.Receivers;
using NuClear.Messaging.Transports.Kafka;
using NuClear.OperationsProcessing.API.Primary;

namespace NuClear.OperationsProcessing.Transports.Kafka
{
    // TODO: перенести в репозиторий operations
    public sealed class KafkaMessage : MessageBase
    {
        public KafkaMessage(ConsumeResult<Ignore, byte[]> result)
        {
            Id = Guid.NewGuid();
            Result = result;
        }

        public override Guid Id { get; }
        public ConsumeResult<Ignore, byte[]> Result { get; }

        public Message<Ignore, byte[]> Message => Result.Message;
    }
    
    public sealed class KafkaReceiver : MessageReceiverBase<KafkaMessage, IPerformedOperationsReceiverSettings>
    {
        private readonly IKafkaMessageFlowReceiver _messageFlowReceiver;

        public KafkaReceiver(
            MessageFlowMetadata sourceFlowMetadata,
            IPerformedOperationsReceiverSettings messageReceiverSettings,
            IKafkaMessageFlowReceiverFactory messageFlowReceiverFactory)
            : base(sourceFlowMetadata, messageReceiverSettings)
        {
            _messageFlowReceiver = messageFlowReceiverFactory.Create(SourceFlowMetadata.MessageFlow);
        }

        protected override IReadOnlyList<KafkaMessage> Peek()
        {
            return _messageFlowReceiver.ReceiveBatch(MessageReceiverSettings.BatchSize).Select(x => new KafkaMessage(x)).ToList();
        }

        protected override void Complete(IEnumerable<KafkaMessage> successfullyProcessedMessages, IEnumerable<KafkaMessage> failedProcessedMessages)
        {
            if (failedProcessedMessages.Any())
            {
                throw new ArgumentException("Kafka processing stopped, some messages cannot be processed");
            }

            _messageFlowReceiver.CompleteBatch(successfullyProcessedMessages.Select(x => x.Result));
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _messageFlowReceiver.Dispose();
            }

            base.OnDispose(disposing);
        }
    }
}