using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Newtonsoft.Json;
using NuClear.Messaging.API.Flows;
using NuClear.Messaging.Transports.Kafka;
using NuClear.River.Hosting.Common.Settings;

namespace NuClear.ValidationRules.Hosting.Common.Settings.Kafka
{
    // offset думаю передавать не в конструкторе, а в connection string
    public sealed class KafkaSettingsFactory : IKafkaSettingsFactory
    {
        private readonly Dictionary<IMessageFlow, KafkaMessageFlowReceiverSettings> _flows2ConsumerSettingsMap = new Dictionary<IMessageFlow, KafkaMessageFlowReceiverSettings>();
        
        public KafkaSettingsFactory(
            IReadOnlyDictionary<IMessageFlow, string> messageFlows2ConnectionStringsMap,
            IEnvironmentSettings environmentSettings)
        {
            foreach (var entry in messageFlows2ConnectionStringsMap)
            {
                var kafkaConfig = ParseConnectionString(entry.Value);
                
                // example group.id: '4f04437a-2f10-4a37-bb49-03810346ae84-Test.11'
                kafkaConfig.Config["group.id"] = entry.Key.Id.ToString() + '-' + environmentSettings.EnvironmentName;

                _flows2ConsumerSettingsMap.Add(entry.Key, kafkaConfig);   
            }
        }

        public IKafkaMessageFlowReceiverSettings CreateReceiverSettings(IMessageFlow messageFlow)
        {
            if (!_flows2ConsumerSettingsMap.TryGetValue(messageFlow, out var settings))
            {
                throw new ArgumentOutOfRangeException($"Can't create kafka info settings. Specified message flow \"{messageFlow.GetType().Name}\" doesn't has appropriate config");
            }

            return settings;
        }

        private static KafkaMessageFlowReceiverSettings ParseConnectionString(string connectionString)
        {
            const string topic = "topic";
            const string pollTimeout = "pollTimeout";

            var settings = new KafkaMessageFlowReceiverSettings
            {
                Config = JsonConvert.DeserializeObject<Dictionary<string, string>>(connectionString)
            };

            // Topic (required)
            if (!settings.Config.TryGetValue(topic, out var rawTopic))
            {
                throw new InvalidOperationException($"Required parameter \"{topic}\" was not found. ConnectionString: {connectionString}");
            }
            
            settings.TopicPartitionOffset = ParseTopicPartitionOffset(rawTopic);
            settings.Config.Remove(topic);

            // PollTimeout (optional)
            if (settings.Config.TryGetValue(pollTimeout, out var rawPollTimeout))
            {
                settings.PollTimeout = TimeSpan.Parse(rawPollTimeout);
                settings.Config.Remove(pollTimeout);
            }

            return settings;
        }

        private static TopicPartitionOffset ParseTopicPartitionOffset(string rawTopic)
        {
            var split = rawTopic.Split(' ');
            var topic = split[0];
            
            var rawPartition = split[1].Trim('[').Trim(']');
            var partition = string.Equals(rawPartition, "Any", StringComparison.OrdinalIgnoreCase) ? Partition.Any : (Partition)int.Parse(rawPartition);

            var rawOffset = split[2].Trim('@');
            
            var offset = string.Equals(rawOffset, "Beginning", StringComparison.OrdinalIgnoreCase) ? Offset.Beginning :
                string.Equals(rawOffset, "End", StringComparison.OrdinalIgnoreCase) ? Offset.End :
                string.Equals(rawOffset, "Unset", StringComparison.OrdinalIgnoreCase) ? Offset.Unset :
                throw new ArgumentOutOfRangeException();
            
            return new TopicPartitionOffset(topic, partition, offset);
        }
        
        private sealed class KafkaMessageFlowReceiverSettings : IKafkaMessageFlowReceiverSettings
        {
            public Dictionary<string, string> Config { get; set; }
            IReadOnlyDictionary<string, string> IKafkaMessageFlowReceiverSettings.Config => Config;
            
            public TopicPartitionOffset TopicPartitionOffset { get; set; }
            public TimeSpan PollTimeout { get; set; } = TimeSpan.FromSeconds(5);
        }
    }
}
