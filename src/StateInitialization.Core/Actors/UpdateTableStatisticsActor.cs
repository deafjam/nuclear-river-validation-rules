using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.Storage;

namespace NuClear.StateInitialization.Core.Actors
{
    public sealed class UpdateTableStatisticsActor : IActor
    {
        private readonly SqlConnection _sqlConnection;

        public UpdateTableStatisticsActor(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            foreach (var command in commands.OfType<UpdateTableStatisticsCommand>())
            {
                UpdateTableStatistics(command.Table);
            }

            return Array.Empty<IEvent>();
        }

        private void UpdateTableStatistics(TableName tableName)
        {
            try
            {
                var database = _sqlConnection.GetDatabase();
                var table = database.GetTable(tableName);
                table.UpdateStatistics();
            }
            catch (Exception ex)
            {
                throw new DataException($"Error occured while statistics updating for table {tableName}", ex);
            }
        }
    }
}