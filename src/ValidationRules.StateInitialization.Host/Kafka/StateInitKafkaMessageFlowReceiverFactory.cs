using System;
using System.Collections.Generic;
using Confluent.Kafka;
using NuClear.Messaging.API.Flows;
using NuClear.Messaging.Transports.Kafka;
using NuClear.Tracing.API;
using NuClear.ValidationRules.Hosting.Common;
using NuClear.ValidationRules.Hosting.Common.Settings.Kafka;

namespace NuClear.ValidationRules.StateInitialization.Host.Kafka
{
    // получаем Receiver, который всегда начинает читать с Offset.Beginning, это полезно для stateinit
    internal sealed class StateInitKafkaMessageFlowReceiverFactory: IKafkaMessageFlowReceiverFactory
    {
        private readonly ITracer _tracer;
        private readonly IKafkaSettingsFactory _kafkaSettingsFactory;


        public StateInitKafkaMessageFlowReceiverFactory(ITracer tracer, IKafkaSettingsFactory kafkaSettingsFactory)
        {
            _tracer = tracer;
            _kafkaSettingsFactory = kafkaSettingsFactory;
        }


        public IKafkaMessageFlowReceiver Create(IMessageFlow messageFlow)
        {
            var settings = _kafkaSettingsFactory.CreateReceiverSettings(messageFlow);
            var stateInitSettings = new StateInitKafkaMessageFlowReceiverSettings(settings);
          
            return new KafkaMessageFlowReceiver(stateInitSettings, _tracer);
        }

        private sealed class StateInitKafkaMessageFlowReceiverSettings : IKafkaMessageFlowReceiverSettings
        {
            private readonly IKafkaMessageFlowReceiverSettings _wrap;

            public StateInitKafkaMessageFlowReceiverSettings(IKafkaMessageFlowReceiverSettings wrap) => _wrap = wrap;

            public IReadOnlyDictionary<string, string> Config => _wrap.Config;
            public TopicPartitionOffset TopicPartitionOffset => new TopicPartitionOffset(_wrap.TopicPartitionOffset.TopicPartition, Offset.Beginning);
            public TimeSpan PollTimeout => _wrap.PollTimeout;
        }
    }
}