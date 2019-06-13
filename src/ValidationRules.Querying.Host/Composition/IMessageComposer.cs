using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Composition
{
    public interface IMessageComposer
    {
        MessageTypeCode MessageType { get; }
        MessageComposerResult Compose(NamedReference[] references, IReadOnlyDictionary<string, string> extra);
    }

    public class MessageComposerResult
    {
        public MessageComposerResult(NamedReference mainReference, string template, params NamedReference[] references)
        {
            MainReference = mainReference;
            Template = template;
            References = references;
        }

        public MessageComposerResult(NamedReference mainReference, string template, params object[] args)
        {
            var templateArgs = args.Aggregate((List: new List<object>(), Index: 0), (tuple, x) =>
            {
                (x, tuple.Index) = PrepareTemplateParameter(x, tuple.Index);
                tuple.List.Add(x);
                return tuple;
            }).List.ToArray();

            MainReference = mainReference;
            Template = string.Format(template, templateArgs);
            References = args.OfType<NamedReference>().ToList();
        }

        public MessageComposerResult(NamedReference mainReference, string template)
            :this(mainReference, template, Array.Empty<NamedReference>())
        {
        }

        public NamedReference MainReference { get; set; }
        public string Template { get; set; }
        public IReadOnlyCollection<NamedReference> References { get; set; }

        private static (object, int) PrepareTemplateParameter(object p, int index)
        {
            switch (p)
            {
                case string str:
                    return (str.Replace("{", "{{").Replace("}", "}}"), index);
                case NamedReference _:
                    return ($"{{{index}}}", index + 1);
                default:
                    return (p, index);
            }
        }
    }
}