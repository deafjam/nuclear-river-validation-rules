using System;

namespace NuClear.ValidationRules.Storage.Model.Events
{
    public sealed class EventRecord
    {
        public long Id { get; set; }
        public Guid Flow { get; set; }
        public string Content { get; set; }
    }
}