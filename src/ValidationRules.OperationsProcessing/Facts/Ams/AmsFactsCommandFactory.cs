using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using NuClear.OperationsProcessing.Transports.Kafka;
using NuClear.Replication.Core;
using NuClear.ValidationRules.Replication;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Dto;
using NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.Ams
{
    internal sealed class AmsFactsCommandFactory : ICommandFactory<KafkaMessage>
    {
        private readonly IDeserializer<ConsumeResult<Ignore, byte[]>, AdvertisementDto> _deserializer = new AdvertisementDtoDeserializer();

        public IEnumerable<ICommand> CreateCommands(KafkaMessage kafkaMessage)
        {
            yield return new IncrementAmsStateCommand(new AmsState(kafkaMessage.Result.Offset, kafkaMessage.Message.Timestamp.UtcDateTime));

            var dtos = _deserializer.Deserialize(new[] {kafkaMessage.Result}).ToList(); 
            if (dtos.Count > 0)
            {
                yield return new ReplaceDataObjectCommand(typeof(Advertisement), dtos);
            }
        }
    }
}