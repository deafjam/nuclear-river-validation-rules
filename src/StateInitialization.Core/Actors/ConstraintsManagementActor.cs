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
    public sealed class ConstraintsManagementActor : IActor
    {
        private readonly SqlConnection _sqlConnection;
        private readonly int _commandTimeout;

        public ConstraintsManagementActor(SqlConnection sqlConnection, TimeSpan commandTimeout)
        {
            _sqlConnection = sqlConnection;
            _commandTimeout = (int)commandTimeout.TotalSeconds;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            if (commands.OfType<DisableConstraintsCommand>().Any())
            {
                var tables = GetTables();

                var defaults = tables.SelectMany(t => t.Columns.Cast<Column>().Select(c => c.DefaultConstraint))
                    .Where(d => d != null)
                    .ToArray();
                var defaultsToRestore = new List<StringCollection>();
                foreach (var defaultConstraint in defaults)
                {
                    defaultsToRestore.Add(defaultConstraint.Script());
                    defaultConstraint.Drop();
                }

                var checks = tables.SelectMany(x => x.Checks.Cast<Check>()).ToArray();
                var checksToRestore = new List<StringCollection>();
                foreach (var check in checks)
                {
                    checksToRestore.Add(check.Script());
                    check.Drop();
                }

                var foreignKeys = tables.SelectMany(x => x.ForeignKeys.Cast<ForeignKey>()).ToArray();
                var foreignKeysToRestore = new List<StringCollection>();
                foreach (var foreignKey in foreignKeys)
                {
                    foreignKeysToRestore.Add(foreignKey.Script());
                    foreignKey.Drop();
                }

                return new[] { new ConstraintsDisabledEvent(checksToRestore, defaultsToRestore, foreignKeysToRestore) };
            }

            var enableCommand = commands.OfType<EnableConstraintsCommand>().SingleOrDefault();
            if (enableCommand != null)
            {
                foreach (var check in enableCommand.ChecksToRestore)
                {
                    foreach (var script in check)
                    {
                        var command = new SqlCommand(script, _sqlConnection) { CommandTimeout = _commandTimeout };
                        command.ExecuteNonQuery();
                    }
                }

                foreach (var defaultToRestore in enableCommand.DefaultsToRestore)
                {
                    foreach (var script in defaultToRestore)
                    {
                        var command = new SqlCommand(script, _sqlConnection) { CommandTimeout = _commandTimeout };
                        command.ExecuteNonQuery();
                    }
                }

                foreach (var foreignKey in enableCommand.ForeignKeysToRestore)
                {
                    foreach (var script in foreignKey)
                    {
                        var command = new SqlCommand(script, _sqlConnection) { CommandTimeout = _commandTimeout };
                        command.ExecuteNonQuery();
                    }
                }
            }

            return Array.Empty<IEvent>();
        }

        private IReadOnlyCollection<Table> GetTables()
        {
            var database = _sqlConnection.GetDatabase();
            return database.Tables.OfType<Table>().Where(x => !x.IsSystemObject).ToArray();
        }
    }
}