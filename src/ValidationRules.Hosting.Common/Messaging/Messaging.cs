using System;
using System.Collections.Generic;
using Confluent.Kafka;
using NuClear.Messaging.API.Flows;
using NuClear.Settings.API;

namespace NuClear.Messaging.Transports.Kafka
{
    // TODO: перенести в репозиторий messaging
    public sealed class KafkaAdminSettings : ISettings
    {
        public IReadOnlyDictionary<string, string> Config { get; set; }
    }

    public sealed class KafkaMessageFlowReceiverSettings : ISettings
    {
        public IReadOnlyDictionary<string, string> Config { get; set; }
        public IEnumerable<string> Topics { get; set; }
        public Offset Offset { get; set; } = Offset.Unset;
        public TimeSpan PollTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }

    public interface IKafkaMessageFlowReceiverFactory
    {
        IKafkaMessageFlowReceiver Create(params IMessageFlow[] messageFlows);
    }

    public interface IKafkaMessageFlowReceiver : IDisposable
    {
        IReadOnlyCollection<ConsumeResult<Ignore, byte[]>> ReceiveBatch(int batchSize);
        void CompleteBatch(IEnumerable<ConsumeResult<Ignore, byte[]>> batch);
    }
}