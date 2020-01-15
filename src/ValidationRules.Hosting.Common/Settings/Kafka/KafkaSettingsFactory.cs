using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NuClear.Messaging.API.Flows;
using NuClear.Messaging.Transports.Kafka;
using NuClear.River.Hosting.Common.Identities.Connections;
using NuClear.Settings;
using NuClear.Storage.API.ConnectionStrings;

namespace NuClear.ValidationRules.Hosting.Common.Settings.Kafka
{
    public sealed class KafkaSettingsFactory : IKafkaSettingsFactory
    {
        private readonly IReadOnlyDictionary<string, string> _kafkaConfig;
        
        public KafkaSettingsFactory(IConnectionStringSettings connectionStringSettings)
        {
            var kafkaConfig = JsonConvert.DeserializeObject<Dictionary<string, string>>(connectionStringSettings.GetConnectionString(KafkaConnectionStringIdentity.Instance));
            _kafkaConfig = kafkaConfig;
        }

        public KafkaAdminSettings CreateAdminSettings() =>
            new KafkaAdminSettings
            {
                Config = _kafkaConfig
            };

        public KafkaMessageFlowReceiverSettings CreateReceiverSettings(IMessageFlow[] messageFlows)
        {
            var topics = new List<string>();
            
            if (messageFlows.Contains(AmsFactsFlow.Instance))
            {
                topics.Add(ConfigFileSetting.String.Required("AmsFactsTopic").Value);
            }
            if (messageFlows.Contains(RulesetFactsFlow.Instance))
            {
                topics.Add(ConfigFileSetting.String.Required("RulesetFactsTopic").Value);
            }

            if (topics.Count == 0)
            {
                throw new ArgumentException($"Unknown message flows provided");                
            }

            return new KafkaMessageFlowReceiverSettings
            {
                Config = _kafkaConfig,
                Topics = topics
            };
        }
    }
}