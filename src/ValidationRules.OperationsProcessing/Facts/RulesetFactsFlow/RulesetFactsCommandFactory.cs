using System;
using System.Collections.Generic;

using NuClear.OperationsProcessing.Transports.Kafka;
using NuClear.Replication.Core;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Dto;
using NuClear.ValidationRules.Storage.Model.Facts;

using ValidationRules.Hosting.Common.Settings;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.RulesetFactsFlow
{
    internal sealed class RulesetFactsCommandFactory : ICommandFactory<KafkaMessage>
    {
        private readonly IDeserializer<Confluent.Kafka.Message, RulesetDto> _deserializer;

        public RulesetFactsCommandFactory(IBusinessModelSettings businessModelSettings)
        {
            _deserializer = new RulesetDtoDeserializer(businessModelSettings);
        }

        IEnumerable<ICommand> ICommandFactory<KafkaMessage>.CreateCommands(KafkaMessage kafkaMessage)
        {
            var deserializedDtos = _deserializer.Deserialize(kafkaMessage.Message);
            if (deserializedDtos.Count != 0)
            {
                yield return new ReplaceDataObjectCommand(typeof(Ruleset), deserializedDtos);
                yield return new ReplaceDataObjectCommand(typeof(Ruleset.AssociatedRule), deserializedDtos);
                yield return new ReplaceDataObjectCommand(typeof(Ruleset.DeniedRule), deserializedDtos);
                yield return new ReplaceDataObjectCommand(typeof(Ruleset.QuantitativeRule), deserializedDtos);
                yield return new ReplaceDataObjectCommand(typeof(Ruleset.RulesetProject), deserializedDtos);
            }
        }
    }
}