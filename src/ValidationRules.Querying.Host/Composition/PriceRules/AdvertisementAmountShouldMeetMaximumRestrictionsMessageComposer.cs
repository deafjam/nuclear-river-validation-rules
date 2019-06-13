using System.Collections.Generic;
using NuClear.ValidationRules.Querying.Host.Properties;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Composition.PriceRules
{
    public sealed class AdvertisementAmountShouldMeetMaximumRestrictionsMessageComposer : IMessageComposer, IDistinctor
    {
        public MessageTypeCode MessageType => MessageTypeCode.AdvertisementAmountShouldMeetMaximumRestrictions;

        public MessageComposerResult Compose(NamedReference[] references, IReadOnlyDictionary<string, string> extra)
        {
            var orderReference = references.Get<EntityTypeOrder>();
            var nomenclatureCategoryReference = references.Get<EntityTypeNomenclatureCategory>();

            var dto = extra.ReadAdvertisementCountMessage();
            var period = dto.Begin.AddMonths(1) == dto.End
                ? dto.Begin.ToString("MMMM")
                : $"{dto.Begin:MMMM} - {dto.End:MMMM}";

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