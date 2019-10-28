using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Events;

namespace NuClear.StateInitialization.Core.Actors
{
    public class ViewManagementActor : IActor
    {
        private readonly SqlConnection _sqlConnection;
        private readonly int _commandTimeout;

        public ViewManagementActor(SqlConnection sqlConnection, TimeSpan commandTimeout)
        {
            _sqlConnection = sqlConnection;
            _commandTimeout = (int)commandTimeout.TotalSeconds;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            if (commands.OfType<DropViewsCommand>().Any())
            {
                var database = _sqlConnection.GetDatabase();
                var views = database.Views.Cast<View>().Where(v => v != null && !v.IsSystemObject).ToArray();

                var viewsToRestore = new List<StringCollection>();
                foreach (var view in views)
                {
                    viewsToRestore.Add(view.Script());
                    view.Drop();
                }

                return new[] { new ViewsDroppedEvent(viewsToRestore) };
            }

            var restoreViewsCommand = commands.OfType<RestoreViewsCommand>().SingleOrDefault();
            if (restoreViewsCommand != null)
            {
                foreach (var view in restoreViewsCommand.ViewsToRestore)
                {
                    foreach (var script in view)
                    {
                        var command = new SqlCommand(script, _sqlConnection) { CommandTimeout = _commandTimeout };
                        command.ExecuteNonQuery();
                    }
                }
            }

            return Array.Empty<IEvent>();
        }
    }
}