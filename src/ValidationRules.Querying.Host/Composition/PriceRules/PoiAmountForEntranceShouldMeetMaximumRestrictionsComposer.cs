using System.Collections.Generic;
using System.Linq;
using NuClear.ValidationRules.Querying.Host.Properties;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Composition.PriceRules
{
    public sealed class PoiAmountForEntranceShouldMeetMaximumRestrictionsComposer : IMessageComposer, IDistinctor
    {
        public MessageTypeCode MessageType => MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions;

        public MessageComposerResult Compose(NamedReference[] references, IReadOnlyDictionary<string, string> extra)
        {
            var maxCount = extra["maxCount"];
            var entranceCode = extra["entranceCode"];
            var period = extra.ExtractPeriod();

            var orders = references.GetMany<EntityTypeOrder>().ToList();
            var firmAddress = references.Get<EntityTypeFirmAddress>();

            var currentOrder = orders[0];
            var conflictingOrders = orders.Skip(1).ToList();

            var conflictingOrderPlaceholders = Enumerable.Range(4, conflictingOrders.Count).Select(i => $"{{{i}}}");
            var template = Resources.PoiLimitExceededForTheEntrance.Replace("{4}", string.Join(", ", conflictingOrderPlaceholders));
            var args = new object[] { maxCount, period, firmAddress, entranceCode }.Concat(conflictingOrders).ToArray();

            return new MessageComposerResult(currentOrder,
                                             template,
                                             args);
        }

        public IEnumerable<Message> Distinct(IEnumerable<Message> messages)
        {
            return messages.GroupBy(x => new
                                        {
                                            x.OrderId,
                                            FirmAddressId = x.References.Get<EntityTypeFirmAddress>().Id,
                                            MaxCount = x.Extra["maxCount"],
                                            EntranceCode = x.Extra["entranceCode"]
                                        })
                           .Select(x => new Message
                               {
                                   MessageType = MessageType,
                                   OrderId = x.Key.OrderId,
                                   Extra = x.UnionPeriod(new Dictionary<string, string>
                                   {
                                       ["maxCount"] = x.Key.MaxCount,
                                       ["entranceCode"] = x.Key.EntranceCode
                                   }),
                                   References = new[] { new Reference<EntityTypeOrder>(x.Key.OrderId.Value) }.Concat(x.SelectMany(y => y.References)).ToHashSet(Reference.Comparer)
                               });
        }
    }
}