using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Microsoft.SqlServer.Management.Smo;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class ReplaceTableActor : IActor
    {
        private readonly SqlConnection _sqlConnection;

        public ReplaceTableActor(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var replaceCommands = commands.OfType<ReplaceTableCommand>().ToList();
            if (replaceCommands.Count > 0)
            {
                var database = _sqlConnection.GetDatabase();

                // Replacing tables:
                foreach (var command in replaceCommands)
                {
                    try
                    {
                        var tableToReplace = database.GetTable(command.TableToReplace);
                        var replacementTable = database.GetTable(command.ReplacementTable);

                        tableToReplace.Drop();

                        var indexesCount = RenameIndexesBack(replacementTable);

                        replacementTable.Rename(tableToReplace.Name);

                        Console.WriteLine($"[{DateTime.Now}] Replaced table {command.TableToReplace} with table {command.ReplacementTable} ({indexesCount} indexes)");
                    }
                    catch (Exception ex)
                    {
                        throw new DataException($"Error occured while replacing table {command.TableToReplace} with {command.ReplacementTable}", ex);
                    }
                }
            }

            return Array.Empty<IEvent>();
        }

        private static int RenameIndexesBack(Table table)
        {
            var indexes = table.Indexes.OfType<Index>().ToArray();
            foreach (var index in indexes)
            {
                if (index.ExtendedProperties.Contains("CopyOf"))
                {
                    var oldNameProperty = index.ExtendedProperties["CopyOf"];
                    index.Rename((string)oldNameProperty.Value);
                    oldNameProperty.Drop();
                }
            }

            return indexes.Length;
        }
    }
}
