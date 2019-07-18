using System.Collections.Generic;
using System.Linq;
using NuClear.ValidationRules.Querying.Host.Properties;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Composition.PriceRules
{
    public sealed class AdvertisementAmountShouldMeetRestrictionsMassMessageComposer : IMessageComposer, IDistinctor
    {
        public MessageTypeCode MessageType => MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictionsMass;

        public MessageComposerResult Compose(NamedReference[] references, IReadOnlyDictionary<string, string> extra)
        {
            var projectReference = references.Get<EntityTypeProject>();
            var nomenclatureCategoryReference = references.Get<EntityTypeNomenclatureCategory>();

            var dto = extra.ReadAdvertisementCountMessage();
            var period = extra.ExtractPeriod();

            return new MessageComposerResult(
                projectReference,
                Resources.AdvertisementAmountShouldMeetMinimumRestrictions,
                nomenclatureCategoryReference.Name,
                dto.Min,
                dto.Max,
                period,
                dto.Count);
        }

        public IEnumerable<Message> Distinct(IEnumerable<Message> messages)
            => messages.GroupBy(x => new
            {
                x.OrderId,
                NomenclatureCategoryId = x.References.Get<EntityTypeNomenclatureCategory>().Id,
            }).Select(x => Merge(x.ToList()));

        private static Message Merge(IReadOnlyCollection<Message> messages)
        {
            var first = messages.First();
            first.Extra = messages.UnionPeriod(first.Extra);

            return first;
        }
    }
}