using System.IO;

using Microsoft.ServiceBus.Messaging;

using NuClear.OperationsLogging.Transports.ServiceBus;
using NuClear.Replication.Core;

namespace NuClear.Replication.OperationsProcessing.Transports.ServiceBus
{
    public sealed class BinaryEvent2BrokeredMessageConverter : IEvent2BrokeredMessageConverter<IEvent>
    {
        private readonly IBinaryEventSerializer _serializer;

        public BinaryEvent2BrokeredMessageConverter(IBinaryEventSerializer serializer)
        {
            _serializer = serializer;
        }

        public BrokeredMessage Convert(IEvent @event)
        {
            var stream = new MemoryStream(_serializer.Serialize(@event));
            return new BrokeredMessage(stream, true);
        }
    }
}