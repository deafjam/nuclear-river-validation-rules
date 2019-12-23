using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Newtonsoft.Json;
using NuClear.Messaging.API.Flows;
using NuClear.Messaging.Transports.Kafka;
using NuClear.River.Hosting.Common.Settings;

namespace NuClear.ValidationRules.Hosting.Common.Settings.Kafka
{
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
                kafkaConfig.Config["group.id"] = string.Concat(entry.Key.Id.ToString(), "-", environmentSettings.EnvironmentName);

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
            const string Topic = "topic";
            const string PollTimeout = "pollTimeout";

            var settings = new KafkaMessageFlowReceiverSettings
            {
                Config = JsonConvert.DeserializeObject<Dictionary<string, string>>(connectionString)
            };

            // Topic (required)
            if (!settings.Config.TryGetValue(Topic, out var rawTopic))
            {
                throw new InvalidOperationException($"Required parameter \"{Topic}\" was not found. ConnectionString: {connectionString}");
            }
            
            settings.TopicPartitionOffset = ParseTopicPartitionOffset(rawTopic);
            settings.Config.Remove(Topic);

            // PollTimeout (optional)
            if (settings.Config.TryGetValue(PollTimeout, out var rawPollTimeout))
            {
                settings.PollTimeout = TimeSpan.Parse(rawPollTimeout);
                settings.Config.Remove(PollTimeout);
            }

            return settings;
        }

        private static TopicPartitionOffset ParseTopicPartitionOffset(string rawTopic)
        {
            var split = rawTopic.Split(' ');
            var topic = split[0];

            Partition partition;
            if (split.Length <= 1)
            {
                partition = Partition.Any;
            }
            else
            {
                var rawPartition = split[1].Trim('[').Trim(']');
                partition = string.Equals(rawPartition, "Any", StringComparison.OrdinalIgnoreCase) ? Partition.Any : (Partition)int.Parse(rawPartition);
            }

            Offset offset;
            if (split.Length <= 2)
            {
                offset = Offset.Unset; 
            }
            else
            {
                var rawOffset = split[2].Trim('@');
            
                offset = string.Equals(rawOffset, "Beginning", StringComparison.OrdinalIgnoreCase) ? Offset.Beginning :
                    string.Equals(rawOffset, "End", StringComparison.OrdinalIgnoreCase) ? Offset.End :
                    string.Equals(rawOffset, "Unset", StringComparison.OrdinalIgnoreCase) ? Offset.Unset :
                    throw new ArgumentOutOfRangeException();
            }
            
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
