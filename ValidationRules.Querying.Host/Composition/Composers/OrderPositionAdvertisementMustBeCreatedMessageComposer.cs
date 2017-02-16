﻿using System.Collections.Generic;

using NuClear.ValidationRules.Querying.Host.Properties;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Composition.Composers
{
    public sealed class OrderPositionAdvertisementMustBeCreatedMessageComposer : IMessageComposer
    {
        public MessageTypeCode MessageType => MessageTypeCode.OrderPositionAdvertisementMustBeCreated;

        public MessageComposerResult Compose(NamedReference[] references, IReadOnlyDictionary<string, string> extra)
        {
            var orderPosition = (OrderPositionNamedReference)references.Get<EntityTypeOrderPosition>();
            var orderPositionAdvertisement = references.Get<EntityTypeOrderPositionAdvertisement>();

            return new MessageComposerResult(
                orderPosition.Order,
                Resources.OrderCheckCompositePositionMustHaveLinkingObject,
                orderPosition,
                orderPositionAdvertisement);
        }
    }
}