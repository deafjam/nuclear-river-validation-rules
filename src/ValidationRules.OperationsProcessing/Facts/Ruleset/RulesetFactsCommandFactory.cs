using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using NuClear.OperationsProcessing.Transports.Kafka;
using NuClear.Replication.Core;
using NuClear.ValidationRules.Hosting.Common.Settings;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Dto;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.Ruleset
{
    internal sealed class RulesetFactsCommandFactory : ICommandFactory<KafkaMessage>
    {
        private readonly IDeserializer<ConsumeResult<Ignore, byte[]>, RulesetDto> _deserializer;

        public RulesetFactsCommandFactory()
        {
            _deserializer = new RulesetDtoDeserializer();
        }

        IEnumerable<ICommand> ICommandFactory<KafkaMessage>.CreateCommands(KafkaMessage kafkaMessage)
        {
            var deserializedDtos = _deserializer.Deserialize(new [] {kafkaMessage.Result}).ToList();
            if (deserializedDtos.Count != 0)
            {
                yield return new ReplaceDataObjectCommand(typeof(Storage.Model.Facts.Ruleset), deserializedDtos);
                yield return new ReplaceDataObjectCommand(typeof(Storage.Model.Facts.Ruleset.AssociatedRule), deserializedDtos);
                yield return new ReplaceDataObjectCommand(typeof(Storage.Model.Facts.Ruleset.DeniedRule), deserializedDtos);
                yield return new ReplaceDataObjectCommand(typeof(Storage.Model.Facts.Ruleset.QuantitativeRule), deserializedDtos);
                yield return new ReplaceDataObjectCommand(typeof(Storage.Model.Facts.Ruleset.RulesetProject), deserializedDtos);
            }
        }
    }
}
