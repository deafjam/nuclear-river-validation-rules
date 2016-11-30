﻿using NuClear.ValidationRules.Querying.Host.Model;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Composition
{
    public interface IMessageComposer
    {
        MessageTypeCode MessageType { get; }
        MessageComposerResult Compose(Version.ValidationResult validationResult);
    }

    public class MessageComposerResult
    {
        public MessageComposerResult(EntityReference mainReference, string template, params EntityReference[] references)
        {
            MainReference = mainReference;
            Template = template;
            References = references;
        }

        public EntityReference MainReference { get; set; }
        public string Template { get; set; }
        public EntityReference[] References { get; set; }
    }
}