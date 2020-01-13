using System.Collections.Generic;
using System.Linq;
using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json;

using NuClear.ValidationRules.Replication.Dto;

namespace NuClear.ValidationRules.OperationsProcessing.Facts.AmsFactsFlow
{
    public sealed class AdvertisementDtoDeserializer : IDeserializer<ConsumeResult<Ignore, byte[]>, AdvertisementDto>
    {
        public IEnumerable<AdvertisementDto> Deserialize(IEnumerable<ConsumeResult<Ignore, byte[]>> consumeResults) =>
            consumeResults
                // filter heartbeat & tombstone messages
                .Where(x => x.Value != null)
                .Select(x =>
                {
                    var dto = JsonConvert.DeserializeObject<AdvertisementDto>(Encoding.UTF8.GetString(x.Value));
                    dto.Offset = x.Offset;

                    return dto;
                });
    }
}