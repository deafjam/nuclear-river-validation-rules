using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

using NuClear.Replication.Core.Commands;
using NuClear.Replication.Core.DataObjects;
using NuClear.Storage.API.Readings;

namespace NuClear.Replication.Core.Actors
{
    public sealed class ReplaceDataObjectsActor<TDataObject> : IActor
        where TDataObject : class
    {
        private readonly IQuery _query;
        private readonly IBulkRepository<TDataObject> _bulkRepository;
        private readonly IMemoryBasedDataObjectAccessor<TDataObject> _memoryBasedDataObjectAccessor;
        private readonly IDataChangesHandler<TDataObject> _dataChangesHandler;

        public ReplaceDataObjectsActor(
            IQuery query,
            IBulkRepository<TDataObject> bulkRepository,
            IMemoryBasedDataObjectAccessor<TDataObject> memoryBasedDataObjectAccessor,
            IDataChangesHandler<TDataObject> dataChangesHandler)
        {
            _query = query;
            _bulkRepository = bulkRepository;
            _memoryBasedDataObjectAccessor = memoryBasedDataObjectAccessor;
            _dataChangesHandler = dataChangesHandler;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            var commandsToExecute = commands.OfType<IReplaceDataObjectCommand>()
                                            .Where(x => x.DataObjectType == typeof(TDataObject))
                                            .ToHashSet();
            if (commandsToExecute.Count == 0)
            {
                return Array.Empty<IEvent>();
            }

            var events = new List<IEvent>();
            
            using var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.Zero });
            
            var findSpecification = _memoryBasedDataObjectAccessor.GetFindSpecification(commandsToExecute);
            var existingDataObjects = _query.For(findSpecification).ToList();
            if (existingDataObjects.Count != 0)
            {
                events.AddRange(_dataChangesHandler.HandleRelates(existingDataObjects));
                _bulkRepository.Delete(existingDataObjects);
            }

            var targetDataObjects = _memoryBasedDataObjectAccessor.GetDataObjects(commandsToExecute);
            if (targetDataObjects.Count != 0)
            {
                _bulkRepository.Create(targetDataObjects);
                events.AddRange(_dataChangesHandler.HandleCreates(targetDataObjects));
                events.AddRange(_dataChangesHandler.HandleRelates(targetDataObjects));
            }

            transaction.Complete();

            return events;
        }
    }
}