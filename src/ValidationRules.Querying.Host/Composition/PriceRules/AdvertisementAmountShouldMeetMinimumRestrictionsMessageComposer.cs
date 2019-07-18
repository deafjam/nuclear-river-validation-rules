using System.Collections.Generic;
using NuClear.ValidationRules.Querying.Host.Properties;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Composition.PriceRules
{
    public sealed class AdvertisementAmountShouldMeetMinimumRestrictionsMessageComposer : IMessageComposer, IDistinctor
    {
        public MessageTypeCode MessageType => MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictions;

        public MessageComposerResult Compose(NamedReference[] references, IReadOnlyDictionary<string, string> extra)
        {
            var orderReference = references.Get<EntityTypeOrder>();
            var nomenclatureCategoryReference = references.Get<EntityTypeNomenclatureCategory>();

            var dto = extra.ReadAdvertisementCountMessage();
            var period = dto.Start.Month == dto.End.Month
                ? dto.Start.ToString("MMMM")
                : $"{dto.Start:MMMM} - {dto.End:MMMM}";

            return new MessageComposerResult(
                orderReference,
                Resources.AdvertisementAmountShouldMeetMinimumRestrictions,
                nomenclatureCategoryReference.Name,
                dto.Min,
                dto.Max,
                period,
                dto.Count);
        }

        public IEnumerable<Message> Distinct(IEnumerable<Message> messages)
            => messages;
    }
}