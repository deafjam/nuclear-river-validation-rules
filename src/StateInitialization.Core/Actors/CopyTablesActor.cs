using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.DataObjects;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Storage;
using NuClear.Storage.API.ConnectionStrings;

namespace NuClear.StateInitialization.Core.Actors
{
    [Obsolete("Похоже, что этот тип не используется. Не нужно тратить время на его актуализацию, проще удалить.")]
    public sealed class CopyTablesActor : IActor
    {
        private readonly IDataObjectTypesProvider _dataObjectTypesProvider;
        private readonly IConnectionStringSettings _connectionStringSettings;

        public CopyTablesActor(
            IDataObjectTypesProvider dataObjectTypesProvider,
            IConnectionStringSettings connectionStringSettings)
        {
            _dataObjectTypesProvider = dataObjectTypesProvider;
            _connectionStringSettings = connectionStringSettings;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var command in commands.Cast<ReplicateInBulkCommand>())
            {
                using (var db = CreateSqlConnection(command.TargetStorageDescriptor))
                {
                    var actor = new CreateTableCopyActor(db);
                    actor.ExecuteCommands(CreateCopyTableCommands(command));
                }
            }

            return Array.Empty<IEvent>();
        }

        private IReadOnlyCollection<CreateTableCopyCommand> CreateCopyTableCommands(ReplicateInBulkCommand command)
            => _dataObjectTypesProvider.Get(command)
                .Select(t => command.TargetStorageDescriptor.MappingSchema.GetTableName(t))
                .Distinct(TableNameComparer.Instance)
                .Select(CreateCopyCommand)
                .ToArray();

        private CreateTableCopyCommand CreateCopyCommand(TableName t)
            => new CreateTableCopyCommand(t, new TableName($"{t.Schema}_{t.Table}", "dbo"));

        private SqlConnection CreateSqlConnection(StorageDescriptor storageDescriptor)
            => new SqlConnection(_connectionStringSettings.GetConnectionString(storageDescriptor.ConnectionStringIdentity));

        private sealed class TableNameComparer : IEqualityComparer<TableName>
        {
            private static readonly Lazy<TableNameComparer> _instance
                = new Lazy<TableNameComparer>(Create);

            private TableNameComparer()
            {
            }

            public static IEqualityComparer<TableName> Instance
                => _instance.Value;

            private static TableNameComparer Create()
                => new TableNameComparer();

            public bool Equals(TableName x, TableName y)
                => string.Equals(x.Schema, y.Schema, StringComparison.InvariantCultureIgnoreCase)
                   && string.Equals(x.Table, y.Table, StringComparison.InvariantCultureIgnoreCase);

            public int GetHashCode(TableName obj)
                => obj.Schema.GetHashCode() ^ obj.Table.GetHashCode();
        }
    }
}
