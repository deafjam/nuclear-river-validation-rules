using System;
using System.Collections.Generic;
using Confluent.Kafka;
using NuClear.Messaging.API.Flows;
using NuClear.Settings.API;

namespace NuClear.Messaging.Transports.Kafka
{
    // TODO: перенести в репозиторий messaging
    public interface IKafkaMessageFlowReceiverSettings : ISettings
    {
        IReadOnlyDictionary<string, string> Config { get; }

        // no multi-topic support
        TopicPartitionOffset TopicPartitionOffset { get; }

        TimeSpan PollTimeout { get; }
    }

    public interface IKafkaMessageFlowReceiverFactory
    {
        IKafkaMessageFlowReceiver Create(IMessageFlow messageFlow);
    }

    public interface IKafkaMessageFlowReceiver : IDisposable
    {
        IReadOnlyCollection<ConsumeResult<Ignore, byte[]>> ReceiveBatch(int batchSize);
        void CompleteBatch(IEnumerable<ConsumeResult<Ignore, byte[]>> batch);
    }
}