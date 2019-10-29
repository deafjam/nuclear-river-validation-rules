using System.Linq;
using Confluent.Kafka;
using NuClear.Messaging.API.Flows;
using NuClear.ValidationRules.Hosting.Common.Settings.Kafka;

namespace NuClear.ValidationRules.Hosting.Common
{
    public sealed class KafkaMessageFlowInfoProvider
    {
        private readonly IKafkaSettingsFactory _kafkaSettingsFactory;
        private static readonly Partition ZeroPartition = new Partition(0);

        public KafkaMessageFlowInfoProvider(IKafkaSettingsFactory kafkaSettingsFactory)
        {
            _kafkaSettingsFactory = kafkaSettingsFactory;
        }

        public long GetFlowSize(IMessageFlow messageFlow)
        {
            var settings = _kafkaSettingsFactory.CreateReceiverSettings(messageFlow);
            var topicPartition = new TopicPartition(settings.TopicPartitionOffset.Topic, ZeroPartition);

            using var consumer = new ConsumerBuilder<Ignore, Ignore>(settings.Config).Build();
            var offsets = consumer.QueryWatermarkOffsets(topicPartition, settings.PollTimeout);
            return offsets.High;
        }

        public long GetFlowProcessedSize(IMessageFlow messageFlow)
        {
            var settings = _kafkaSettingsFactory.CreateReceiverSettings(messageFlow);
            var topicPartition = new TopicPartition(settings.TopicPartitionOffset.Topic, ZeroPartition);

            using var consumer = new ConsumerBuilder<Ignore, Ignore>(settings.Config).Build();
            var committedOffset = consumer.Committed(new[] { topicPartition }, settings.PollTimeout).First();
            return committedOffset.Offset;
        }

        public ConsumeResult<Ignore, Ignore> TryGetFlowLastMessage(IMessageFlow messageFlow)
        {
            var settings = _kafkaSettingsFactory.CreateReceiverSettings(messageFlow);
            var topicPartition = new TopicPartition(settings.TopicPartitionOffset.Topic, ZeroPartition);

            using var consumer = new ConsumerBuilder<Ignore, Ignore>(settings.Config).Build();
            var offsets = consumer.QueryWatermarkOffsets(topicPartition, settings.PollTimeout);
            consumer.Assign(new[] { new TopicPartitionOffset(topicPartition, offsets.High - 1) });

            return consumer.Consume(settings.PollTimeout);
        }
    }
}