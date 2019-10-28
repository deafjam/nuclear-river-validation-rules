using System.Collections.Generic;

using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;

namespace NuClear.Replication.Core.DataObjects
{
    public sealed class EntityChangesProvider<TDataObject> : IChangesProvider<TDataObject>
        where TDataObject : class
    {
        private readonly IStorageBasedDataObjectAccessor<TDataObject> _storageBasedDataObjectAccessor;
        private readonly TwoPhaseDataChangesDetector<TDataObject> _dataChangesDetector;

        public EntityChangesProvider(IQuery query,
                                     IStorageBasedDataObjectAccessor<TDataObject> storageBasedDataObjectAccessor,
                                     IEqualityComparerFactory equalityComparerFactory)
        {
            _storageBasedDataObjectAccessor = storageBasedDataObjectAccessor;
            _dataChangesDetector = new TwoPhaseDataChangesDetector<TDataObject>(
                                       specification => storageBasedDataObjectAccessor.GetSource().WhereMatched(specification),
                                       specification => query.For<TDataObject>().WhereMatched(specification),
                                       equalityComparerFactory.CreateIdentityComparer<TDataObject>(),
                                       equalityComparerFactory.CreateCompleteComparer<TDataObject>());
        }

        public MergeResult<TDataObject> GetChanges(IReadOnlyCollection<ICommand> commands)
        {
            var specification = _storageBasedDataObjectAccessor.GetFindSpecification(commands);
            return _dataChangesDetector.DetectChanges(specification);
        }
    }
}