﻿using System.Collections.Generic;
using NuClear.ValidationRules.Querying.Host.Properties;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Composition.ConsistencyRules
{
    public sealed class OrderShouldNotBeSignedBeforeBargainMessageComposer : IMessageComposer
    {
        public MessageTypeCode MessageType => MessageTypeCode.OrderShouldNotBeSignedBeforeBargain;

        public MessageComposerResult Compose(NamedReference[] references, IReadOnlyDictionary<string, string> extra)
        {
            var orderReference = references.Get<EntityTypeOrder>();

            return new MessageComposerResult(
                orderReference,
                Resources.OrderShouldNotBeSignedBeforeBargain);
        }
    }
}