using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using NuClear.Messaging.API.Flows;
using NuClear.Replication.Core;
using NuClear.ValidationRules.OperationsProcessing;
using NuClear.ValidationRules.OperationsProcessing.Facts.AmsFactsFlow;
using NuClear.ValidationRules.Replication.Dto;

namespace NuClear.ValidationRules.StateInitialization.Host.Kafka.Ams
{
    public sealed class AmsFactsBulkCommandFactory : IBulkCommandFactory<ConsumeResult<Ignore, byte[]>>
    {
        private readonly IDeserializer<ConsumeResult<Ignore, byte[]>, AdvertisementDto> _deserializer;

        public AmsFactsBulkCommandFactory()
        {
            _deserializer = new AdvertisementDtoDeserializer();
            AppropriateFlows = new[] { AmsFactsFlow.Instance };
        }

        public IReadOnlyCollection<IMessageFlow> AppropriateFlows { get; }

        public IReadOnlyCollection<ICommand> CreateCommands(IReadOnlyCollection<ConsumeResult<Ignore, byte[]>> results)
        {
            var deserializedDtos = _deserializer.Deserialize(results)
                                           .Aggregate(new Dictionary<long, AdvertisementDto>(results.Count),
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
