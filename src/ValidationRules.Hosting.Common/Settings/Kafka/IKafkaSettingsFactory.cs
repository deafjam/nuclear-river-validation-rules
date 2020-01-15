using NuClear.Messaging.API.Flows;
using NuClear.Messaging.Transports.Kafka;

namespace NuClear.ValidationRules.Hosting.Common.Settings.Kafka
{
    public interface IKafkaSettingsFactory
    {
        KafkaAdminSettings CreateAdminSettings();
        KafkaMessageFlowReceiverSettings CreateReceiverSettings(params IMessageFlow[] messageFlows);
    }
}