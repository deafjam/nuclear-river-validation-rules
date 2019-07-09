using System.Collections.Generic;

using NuClear.OperationsProcessing.Transports.Kafka;
using NuClear.Replication.Core;
using NuClear.ValidationRules.Replication;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Dto;
using NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.AmsFactsFlow
{
    internal sealed class AmsFactsCommandFactory : ICommandFactory<KafkaMessage>
    {
        private readonly IDeserializer<Confluent.Kafka.Message, AdvertisementDto> _deserializer = new AdvertisementDtoDeserializer();

        public IEnumerable<ICommand> CreateCommands(KafkaMessage kafkaMessage)
        {
            yield return new IncrementAmsStateCommand(new AmsState(kafkaMessage.Message.Offset, kafkaMessage.Message.Timestamp.UtcDateTime));
            
            var dtos = _deserializer.Deserialize(kafkaMessage.Message);
            if (dtos.Count > 0)
            {
                yield return new ReplaceDataObjectCommand(typeof(Advertisement), dtos);
            }
        }
    }
}