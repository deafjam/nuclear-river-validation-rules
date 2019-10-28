using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using Microsoft.SqlServer.Management.Smo;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class CreateTableCopyActor : IActor
    {
        private readonly Database _database;
        private readonly IndexManager _indexManager;

        public CreateTableCopyActor(SqlConnection sqlConnection)
        {
            _database = sqlConnection.GetDatabase();
            _indexManager = new IndexManager(sqlConnection);
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var command in commands.OfType<CreateTableCopyCommand>())
            {
                var sourceTable = _database.GetTable(command.SourceTable);

                // If table with prefix already exists - drop it:
                var targetTable = _database.GetTable(command.TargetTable);
                if (targetTable != null)
                {
                    targetTable.Drop();
                    Console.WriteLine($"[{DateTime.Now}] [{Environment.CurrentManagedThreadId}] Dropped old shadow copy table {command.TargetTable}");
                }

                var createdTableCopy = CopyTable(sourceTable, command.TargetTable);
                _indexManager.CopyIndexes(sourceTable, createdTableCopy);

                Console.WriteLine($"[{DateTime.Now}] [{Environment.CurrentManagedThreadId}] Created shadow copy of table {command.SourceTable} named {command.TargetTable} with {sourceTable.Indexes.Count} indexes");
            }

            return Array.Empty<IEvent>();
        }

        private Table CopyTable(Table sourceTable, TableName targetTable)
        {
            var newTable = string.IsNullOrEmpty(targetTable.Schema)
                ? new Table(_database, targetTable.Table)
                : new Table(_database, targetTable.Table, targetTable.Schema);

            foreach (Column col in sourceTable.Columns)
            {
                var newCol = new Column(newTable, col.Name, col.DataType, col.IsFileStream)
                {
                    Collation = col.Collation,
                    Computed = col.Computed,
                    ComputedText = col.ComputedText,
                    DefaultSchema = col.DefaultSchema,
                    Identity = col.Identity,
                    IdentityIncrement = col.IdentityIncrement,
                    IdentitySeed = col.IdentitySeed,
                    IsColumnSet = col.IsColumnSet,
                    IsFileStream = col.IsFileStream,
                    IsPersisted = col.IsPersisted,
                    IsSparse = col.IsSparse,
                    NotForReplication = col.NotForReplication,
                    Nullable = col.Nullable,
                    RowGuidCol = col.RowGuidCol
                };

                newTable.Columns.Add(newCol);
            }

            newTable.Create();

            return newTable;
        }
    }
}
