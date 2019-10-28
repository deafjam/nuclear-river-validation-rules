using System.Collections.Generic;
using System.Collections.Specialized;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Events
{
    public sealed class ViewsDroppedEvent : IEvent
    {
        public ViewsDroppedEvent(IEnumerable<StringCollection> viewsToRestore)
        {
            ViewsToRestore = viewsToRestore;
        }

        public IEnumerable<StringCollection> ViewsToRestore { get; }
    }
}