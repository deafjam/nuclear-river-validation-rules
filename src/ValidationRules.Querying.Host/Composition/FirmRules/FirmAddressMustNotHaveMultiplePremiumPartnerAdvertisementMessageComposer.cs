using System.Collections.Generic;
using System.Linq;
using NuClear.ValidationRules.Querying.Host.Properties;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Composition.FirmRules
{
    public sealed class FirmAddressShouldNotHaveMultiplePartnerAdvertisementMessageComposer : IMessageComposer, IDistinctor
    {
        public MessageTypeCode MessageType => MessageTypeCode.FirmAddressShouldNotHaveMultiplePartnerAdvertisement;

        public MessageComposerResult Compose(NamedReference[] references, IReadOnlyDictionary<string, string> extra)
        {
            var orderReference = references.Get<EntityTypeOrder>();
            var addressReference = references.Get<EntityTypeFirmAddress>();
            var firmReference = references.Get<EntityTypeFirm>();
            var periods = extra.ExtractPeriods();

            return new MessageComposerResult(
                orderReference,
                Resources.FirmAddressShouldNotHaveMultiplePartnerAdvertisement,
                addressReference,
                firmReference,
                periods);
        }

        public IEnumerable<Message> Distinct(IEnumerable<Message> messages)
            => messages
                .GroupBy(x => new
                    {
                        x.OrderId,
                        FirmAddressId = x.References.Get<EntityTypeFirmAddress>().Id,
                        FirmId = x.References.Get<EntityTypeFirm>().Id
                    })
                .Select(group => Merge(group.ToList()));

        private static Message Merge(IReadOnlyCollection<Message> messages)
        {
            var first = messages.First();
            first.Extra = messages.UnionPeriods(first.Extra); 

            return first;
        }
    }

    // Тип нарочно здесь, поскольку связан с первым.
    public sealed class FirmAddressMustNotHaveMultiplePremiumPartnerAdvertisementMessageComposer : IMessageComposer, IDistinctor
    {
        public MessageTypeCode MessageType => MessageTypeCode.FirmAddressMustNotHaveMultiplePremiumPartnerAdvertisement;

        public MessageComposerResult Compose(NamedReference[] references, IReadOnlyDictionary<string, string> extra)
        {
            var orders = references.GetMany<EntityTypeOrder>().ToList();
            var orderReference = orders.First();
            var conflictOrderReference = orders.Last();
            
            var addressReference = references.Get<EntityTypeFirmAddress>();
            var firmReference = references.Get<EntityTypeFirm>();
            var periods = extra.ExtractPeriods();

            return new MessageComposerResult(
                orderReference,
                Resources.FirmAddressMustNotHaveMultipleCallToAction,
                addressReference,
                firmReference,
                periods,
                conflictOrderReference);
        }

        public IEnumerable<Message> Distinct(IEnumerable<Message> messages)
            => messages
                .GroupBy(x => new
                    {
                        x.OrderId,
                        FirmAddressId = x.References.Get<EntityTypeFirmAddress>().Id,
                        FirmId = x.References.Get<EntityTypeFirm>().Id,
                    })
                .Select(group => Merge(group.ToList()));

        private static Message Merge(IReadOnlyCollection<Message> messages)
        {
            var first = messages.First();
            first.Extra = messages.UnionPeriods(first.Extra); 

            return first;
        }
    }
}