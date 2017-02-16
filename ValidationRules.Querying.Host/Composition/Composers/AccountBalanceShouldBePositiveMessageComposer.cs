﻿using System.Collections.Generic;

using NuClear.ValidationRules.Querying.Host.Properties;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Composition.Composers
{
    public sealed class AccountBalanceShouldBePositiveMessageComposer : IMessageComposer
    {
        public MessageTypeCode MessageType => MessageTypeCode.AccountBalanceShouldBePositive;

        public MessageComposerResult Compose(NamedReference[] references, IReadOnlyDictionary<string, string> extra)
        {
            var orderReference = references.Get<EntityTypeOrder>();
            var dto = extra.ReadAccountBalanceMessage();

            return new MessageComposerResult(
                orderReference,
                string.Format(Resources.OrdersCheckOrderInsufficientFunds, dto.Planned, dto.Available));
        }
    }
}