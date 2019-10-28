using NuClear.Messaging.API.Flows;
using NuClear.Messaging.Transports.Kafka;

namespace NuClear.ValidationRules.Hosting.Common.Settings.Kafka
{
    public interface IKafkaSettingsFactory
    {
        IKafkaMessageFlowReceiverSettings CreateReceiverSettings(IMessageFlow messageFlow);
        IKafkaMessageFlowInfoSettings CreateInfoSettings(IMessageFlow messageFlow);
    }
}