﻿using System.Collections.Generic;

using NuClear.ValidationRules.Querying.Host.Properties;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Composition.Composers
{
    public sealed class OrderPositionMayCorrespontToActualPriceMessageComposer : IMessageComposer
    {
        public MessageTypeCode MessageType => MessageTypeCode.OrderPositionMayCorrespontToActualPrice;

        public MessageComposerResult Compose(NamedReference[] references, IReadOnlyDictionary<string, string> extra)
        {
            var orderPositionReference = (OrderPositionNamedReference)references.Get<EntityTypeOrderPosition>();

            return new MessageComposerResult(
                orderPositionReference.Order,
                Resources.OrderPositionMustCorrespontToActualPrice,
                orderPositionReference);
        }
    }
}