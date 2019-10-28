using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class EnableIndexesActor : IActor
    {
        private readonly IndexManager _indexManager;

        public EnableIndexesActor(SqlConnection sqlConnection)
        {
            _indexManager = new IndexManager(sqlConnection);
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var enableCommand in commands.OfType<EnableIndexesCommand>())
            {
                _indexManager.EnableIndexes(enableCommand.Table);
            }

            return Array.Empty<IEvent>();
        }
    }
}