using NuClear.Messaging.API.Flows;
using NuClear.Messaging.Transports.Kafka;
using NuClear.Tracing.API;
using NuClear.ValidationRules.Hosting.Common.Settings.Kafka;

namespace NuClear.ValidationRules.Hosting.Common
{
    public sealed class KafkaMessageFlowReceiverFactory : IKafkaMessageFlowReceiverFactory
    {
        private readonly ITracer _tracer;
        private readonly IKafkaSettingsFactory _kafkaSettingsFactory;

        public KafkaMessageFlowReceiverFactory(ITracer tracer, IKafkaSettingsFactory kafkaSettingsFactory)
        {
            _tracer = tracer;
            _kafkaSettingsFactory = kafkaSettingsFactory;
        }

        public IKafkaMessageFlowReceiver Create(IMessageFlow[] messageFlows)
        {
            var settings = _kafkaSettingsFactory.CreateReceiverSettings(messageFlows);
            return new KafkaMessageFlowReceiver(settings, _tracer);
        }
    }
}