using System.IO;

using Microsoft.ServiceBus.Messaging;

using NuClear.Messaging.API.Processing.Actors.Transformers;
using NuClear.OperationsProcessing.Transports.ServiceBus;
using NuClear.Replication.Core;

namespace NuClear.Replication.OperationsProcessing.Transports.ServiceBus
{
    public sealed class BrokeredMessage2BinaryEventMessageTransformer : MessageTransformerBase<BrokeredMessageDecorator, EventMessage>
    {
        private readonly IBinaryEventSerializer _eventSerializer;

        public BrokeredMessage2BinaryEventMessageTransformer(IBinaryEventSerializer eventSerializer)
        {
            _eventSerializer = eventSerializer;
        }

        protected override EventMessage Transform(BrokeredMessageDecorator originalMessage)
        {
            return new EventMessage(originalMessage.Id, Transform(originalMessage.Message));
        }

        private IEvent Transform(BrokeredMessage message)
        {
            var bodyStream = message.GetBody<Stream>();
            using (var memory = new MemoryStream())
            {
                bodyStream.CopyTo(memory);
                return _eventSerializer.Deserialize(memory.ToArray());
            }
        }
    }

}