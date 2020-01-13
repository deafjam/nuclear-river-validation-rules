using System.Collections.Generic;
using System.Collections.Specialized;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class RestoreViewsCommand : ICommand
    {
        public RestoreViewsCommand(IEnumerable<StringCollection> viewsToRestore)
        {
            ViewsToRestore = viewsToRestore;
        }

        public IEnumerable<StringCollection> ViewsToRestore { get; }
    }
}