using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using NuClear.Messaging.API.Flows;
using NuClear.Replication.Core;
using NuClear.ValidationRules.Hosting.Common.Settings;
using NuClear.ValidationRules.OperationsProcessing;
using NuClear.ValidationRules.OperationsProcessing.Facts.RulesetFactsFlow;
using NuClear.ValidationRules.Replication.Dto;

namespace NuClear.ValidationRules.StateInitialization.Host.Kafka.Rulesets
{
    public sealed class RulesetFactsBulkCommandFactory : IBulkCommandFactory<ConsumeResult<Ignore, byte[]>>
    {
        private readonly IDeserializer<ConsumeResult<Ignore, byte[]>, RulesetDto> _deserializer;

        public RulesetFactsBulkCommandFactory(IBusinessModelSettings businessModelSettings)
        {
            _deserializer = new RulesetDtoDeserializer(businessModelSettings);
            AppropriateFlows = new[] { RulesetFactsFlow.Instance };
        }

        public IReadOnlyCollection<IMessageFlow> AppropriateFlows { get; }

        public IReadOnlyCollection<ICommand> CreateCommands(IReadOnlyCollection<ConsumeResult<Ignore, byte[]>> messages)
        {
            var deserializedDtos = _deserializer.Deserialize(messages)
                                                .Aggregate(new Dictionary<long, RulesetDto>(messages.Count),
                                                    (dict, dto) =>
                                                    {
                                                        dict[dto.Id] = dto;
                                                        return dict;
                                                    });
            if (deserializedDtos.Count == 0)
            {
                return Array.Empty<ICommand>();
            }

            return DataObjectTypesProvider.RulesetFactTypes
                                                 .Select(factType => new BulkInsertInMemoryDataObjectsCommand(factType, deserializedDtos.Values))
                                                 .ToList();
        }
    }
}
