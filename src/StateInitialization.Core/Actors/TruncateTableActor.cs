using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Data;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class TruncateTableActor : IActor
    {
        private readonly DataConnection _targetDataConnection;

        public TruncateTableActor(DataConnection targetDataConnection)
        {
            _targetDataConnection = targetDataConnection;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var command in commands.OfType<TruncateTableCommand>())
            {
                ExecuteTruncate(command.Table);
            }

            return Array.Empty<IEvent>();
        }

        private void ExecuteTruncate(TableName table)
        {
            _targetDataConnection.Execute($"TRUNCATE TABLE {table}");
        }
    }
}