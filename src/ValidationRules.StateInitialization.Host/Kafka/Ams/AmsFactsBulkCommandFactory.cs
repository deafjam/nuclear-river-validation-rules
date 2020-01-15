using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using NuClear.Replication.Core;
using NuClear.ValidationRules.Hosting.Common.Settings.Kafka;
using NuClear.ValidationRules.OperationsProcessing;
using NuClear.ValidationRules.OperationsProcessing.Facts.Ams;
using NuClear.ValidationRules.Replication.Dto;

namespace NuClear.ValidationRules.StateInitialization.Host.Kafka.Ams
{
    public sealed class AmsFactsBulkCommandFactory : IBulkCommandFactory<ConsumeResult<Ignore, byte[]>>
    {
        private readonly IDeserializer<ConsumeResult<Ignore, byte[]>, AdvertisementDto> _deserializer;
        private readonly IEnumerable<string> _appropriateTopics;
        
        public AmsFactsBulkCommandFactory(IKafkaSettingsFactory kafkaSettingsFactory)
        {
            _appropriateTopics = kafkaSettingsFactory.CreateReceiverSettings(AmsFactsFlow.Instance).Topics;
            _deserializer = new AdvertisementDtoDeserializer();
        }
        
        public IReadOnlyCollection<ICommand> CreateCommands(IReadOnlyCollection<ConsumeResult<Ignore, byte[]>> messages)
        {
            var filtered = messages.Where(x => _appropriateTopics.Contains(x.Topic));
            
            var deserializedDtos = _deserializer.Deserialize(filtered)
                                           .Aggregate(new Dictionary<long, AdvertisementDto>(),
                                                      (dict, dto) =>
                                                          {
                                                              dict[dto.Id] = dto;
                                                              return dict;
                                                          });

            if (deserializedDtos.Count == 0)
            {
                return Array.Empty<ICommand>();
            }

            return DataObjectTypesProvider.AmsFactTypes
                                                 .Select(factType => new BulkInsertInMemoryDataObjectsCommand(factType, deserializedDtos.Values))
                                                 .ToList();
        }
    }
}
