using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class DisableIndexesActor : IActor
    {
        private readonly IndexManager _indexManager;

        public DisableIndexesActor(SqlConnection sqlConnection)
        {
            _indexManager = new IndexManager(sqlConnection);
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach(var disableCommand in commands.OfType<DisableIndexesCommand>())
            {
                _indexManager.DisableIndexes(disableCommand.Table);
            }

            return Array.Empty<IEvent>();
        }
    }
}