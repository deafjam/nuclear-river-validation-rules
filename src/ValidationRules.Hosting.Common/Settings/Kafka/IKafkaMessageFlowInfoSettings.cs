using System;
using System.Collections.Generic;
using Confluent.Kafka;

namespace NuClear.ValidationRules.Hosting.Common.Settings.Kafka
{
    public interface IKafkaMessageFlowInfoSettings
    {
        Dictionary<string, object> Config { get; }
        TopicPartition TopicPartition { get; }
        TimeSpan InfoTimeout { get; }
    }
}