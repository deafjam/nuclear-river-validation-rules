using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Replication.Core.DataObjects;
using NuClear.StateInitialization.Core.Commands;

namespace NuClear.StateInitialization.Core.Actors
{
    internal sealed class BulkInsertDataObjectsActor<TDataObject> : IActor
        where TDataObject : class
    {
        private readonly IStorageBasedDataObjectAccessor<TDataObject> _dataObjectAccessor;
        private readonly DataConnection _targetDataConnection;

        public BulkInsertDataObjectsActor(
            IStorageBasedDataObjectAccessor<TDataObject> dataObjectAccessor,
            DataConnection targetDataConnection)
        {
            _dataObjectAccessor = dataObjectAccessor;
            _targetDataConnection = targetDataConnection;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var command = commands.OfType<BulkInsertDataObjectsCommand>().SingleOrDefault();
            if (command != null)
            {
                ExecuteBulkCopy((int)command.BulkCopyTimeout.TotalSeconds);
            }

            return Array.Empty<IEvent>();
        }
        
        private void ExecuteBulkCopy(int timeout)
        {
            var source = _dataObjectAccessor.GetSource();

            var target = _targetDataConnection.GetTable<TDataObject>();
            var temp = _targetDataConnection.CreateTable<TDataObject>($"#{Guid.NewGuid():N}");

            try
            {
                var options = new BulkCopyOptions { BulkCopyTimeout = timeout };
                temp.BulkCopy(options, source);
                temp.Where(x => !target.Contains(x)).Insert(target, x => x);
            }
            catch (Exception ex)
            {
                string sqlText;
                try
                {
                    var linq2DBQuery = source as IExpressionQuery<TDataObject>;
                    sqlText = linq2DBQuery?.SqlText;
                }
                catch (Exception innerException)
                {
                    sqlText = $"can not build sql query: {innerException.Message}";
                }

                throw new DataException($"Error occured while bulk replacing data for dataobject of type {typeof(TDataObject).Name} using {_dataObjectAccessor.GetType().Name}{Environment.NewLine}{sqlText}{Environment.NewLine}", ex);
            }
        }
    }
}
