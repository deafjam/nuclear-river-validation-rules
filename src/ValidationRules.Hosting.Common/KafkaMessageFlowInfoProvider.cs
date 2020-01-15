using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using NuClear.Messaging.API.Flows;
using NuClear.ValidationRules.Hosting.Common.Settings.Kafka;

namespace NuClear.ValidationRules.Hosting.Common
{
    public sealed class KafkaMessageFlowInfoProvider : IDisposable
    {
        private readonly IAdminClient _adminClient;
        private readonly IKafkaSettingsFactory _kafkaSettingsFactory;
        
        public KafkaMessageFlowInfoProvider(IKafkaSettingsFactory kafkaSettingsFactory)
        {
            _kafkaSettingsFactory = kafkaSettingsFactory;
            _adminClient = new AdminClientBuilder(_kafkaSettingsFactory.CreateAdminSettings().Config).Build();
        }

        public IReadOnlyCollection<MessageFlowStats> GetFlowStats(params IMessageFlow[] messageFlows)
        {
            var settings = _kafkaSettingsFactory.CreateReceiverSettings(messageFlows);
            using var consumer = new ConsumerBuilder<Ignore, Ignore>(settings.Config).Build();

            var topicPartitions = GetTopicPartitions(settings.Topics);
            var stats = consumer.Committed(topicPartitions, settings.PollTimeout).Select(x =>
            {
                var offsets = consumer.QueryWatermarkOffsets(x.TopicPartition, settings.PollTimeout);
                return new MessageFlowStats
                {
                    TopicPartition = x.TopicPartition,
                    
                    End = offsets.High,
                    Offset = x.Offset,
                    Lag = offsets.High - x.Offset
                };
            }).ToList();
            
            return stats;
        }

        // последнее сообщение в topic (в рамках конкретного partition)
        public ConsumeResult<Ignore, Ignore> TryGetFlowLastMessage(IMessageFlow messageFlow, int partitionId = 0)
        {
            var settings = _kafkaSettingsFactory.CreateReceiverSettings(messageFlow);
            var topicPartition = new TopicPartition(settings.Topics.Single(), partitionId);

            using var consumer = new ConsumerBuilder<Ignore, Ignore>(settings.Config).Build();
            var offsets = consumer.QueryWatermarkOffsets(topicPartition, settings.PollTimeout);
            consumer.Assign(new[] { new TopicPartitionOffset(topicPartition, offsets.High - 1) });

            return consumer.Consume(settings.PollTimeout);
        }

        private IEnumerable<TopicPartition> GetTopicPartitions(IEnumerable<string> topics)
        {
            var timeout = TimeSpan.FromSeconds(5);

            var topicPartitions =
                topics
                .Select(x => _adminClient.GetMetadata(x, timeout))
                .SelectMany(x => x.Topics)
                .SelectMany(x => x.Partitions.Select(y => new TopicPartition(x.Topic, y.PartitionId)));

            return topicPartitions;
        }
        
        public void Dispose()
        {
            _adminClient.Dispose();
        }
        
        public struct MessageFlowStats
        {
            public TopicPartition TopicPartition { get; set; }
            public long End { get; set; }
            public long Offset { get; set; }            
            public long Lag { get; set; }
        }
    }
}