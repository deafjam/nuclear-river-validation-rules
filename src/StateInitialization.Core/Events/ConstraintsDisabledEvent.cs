using System.Collections.Generic;
using System.Collections.Specialized;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Events
{
    public sealed class ConstraintsDisabledEvent : IEvent
    {
        public ConstraintsDisabledEvent(
            IReadOnlyCollection<StringCollection> checks,
            IReadOnlyCollection<StringCollection> defaults,
            IReadOnlyCollection<StringCollection> foreignKeys)
        {
            Checks = checks;
            Defaults = defaults;
            ForeignKeys = foreignKeys;
        }

        public IReadOnlyCollection<StringCollection> Checks { get; }
        public IReadOnlyCollection<StringCollection> Defaults { get; }
        public IReadOnlyCollection<StringCollection> ForeignKeys { get; }
    }
}