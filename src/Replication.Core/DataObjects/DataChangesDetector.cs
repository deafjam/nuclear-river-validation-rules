using System;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;

using NuClear.Storage.API.Specifications;

namespace NuClear.Replication.Core.DataObjects
{
    public class DataChangesDetector<T>
    {
        private readonly MapToObjectsSpecProvider<T, T> _sourceProvider;
        private readonly MapToObjectsSpecProvider<T, T> _targetProvider;
        private readonly IEqualityComparer<T> _comparer;

        public DataChangesDetector(
            MapToObjectsSpecProvider<T, T> sourceProvider,
            MapToObjectsSpecProvider<T, T> targetProvider,
            IEqualityComparer<T> comparer)
        {
            _sourceProvider = sourceProvider;
            _targetProvider = targetProvider;
            _comparer = comparer;
        }

        public MergeResult<T> DetectChanges(FindSpecification<T> specification)
        {
            var sourceObjects = new EnumerableDecorator(() => _sourceProvider.Invoke(specification));
            var targetObjects = _targetProvider.Invoke(specification);
            var result = MergeTool.Merge(sourceObjects, targetObjects, _comparer);
            return result;
        }

        private class EnumerableDecorator : IEnumerable<T>
        {
            private readonly Func<IEnumerable<T>> _queryableFactory;

            public EnumerableDecorator(Func<IEnumerable<T>> queryableFactory)
            {
                _queryableFactory = queryableFactory;
            }

            public IEnumerator<T> GetEnumerator()
                => new EnumeratorDecorator(_queryableFactory);

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            private class EnumeratorDecorator : IEnumerator<T>
            {
                private IEnumerator<T> _enumerator;
                private TransactionScope _transaction;
                private readonly Func<IEnumerable<T>> _queryableFactory;

                public EnumeratorDecorator(Func<IEnumerable<T>> queryableFactory)
                {
                    _queryableFactory = queryableFactory;
                }

                public void Dispose()
                {
                    _transaction?.Dispose();
                    _enumerator?.Dispose();
                }

                public bool MoveNext()
                {
                    if (_transaction == null)
                    {
                        _transaction = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });
                        _enumerator = _queryableFactory.Invoke().GetEnumerator();
                    }

                    return _enumerator.MoveNext();
                }

                public void Reset()
                {
                    throw new NotSupportedException();
                }

                public T Current => _enumerator.Current;

                object IEnumerator.Current => Current;
            }
        }
    }
}
