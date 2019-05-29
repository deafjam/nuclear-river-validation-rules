using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;

namespace NuClear.ValidationRules.SingleCheck.Store
{
    public sealed class HashSetStoreFactory : IStoreFactory
    {
        private readonly HashSetStore _store;

        public HashSetStoreFactory(IEqualityComparerFactory equalityComparerFactory) =>
            _store = new HashSetStore(equalityComparerFactory);

        public IStore CreateStore() => _store;

        public IQuery CreateQuery() => _store;

        public void Dispose() { }
    }
}