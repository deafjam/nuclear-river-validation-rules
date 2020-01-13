using System.Collections.Generic;
using System.Collections.Specialized;

using NuClear.Replication.Core;

namespace NuClear.StateInitialization.Core.Commands
{
    public sealed class EnableConstraintsCommand : ICommand
    {
        public EnableConstraintsCommand(
            IReadOnlyCollection<StringCollection> checksToRestore,
            IReadOnlyCollection<StringCollection> defaultsToRestore,
            IReadOnlyCollection<StringCollection> foreignKeysToRestore)
        {
            ChecksToRestore = checksToRestore;
            DefaultsToRestore = defaultsToRestore;
            ForeignKeysToRestore = foreignKeysToRestore;
        }

        public IReadOnlyCollection<StringCollection> ChecksToRestore { get; }
        public IReadOnlyCollection<StringCollection> DefaultsToRestore { get; }
        public IReadOnlyCollection<StringCollection> ForeignKeysToRestore { get; }
    }
}