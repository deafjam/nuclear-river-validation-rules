using LinqToDB.Mapping;
using NuClear.Replication.Core;
using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Storage;
using NuClear.ValidationRules.Storage.FieldComparer;

namespace NuClear.ValidationRules.SingleCheck.Store
{
    public sealed class HashSetStoreFactory : IStoreFactory
    {
        public static readonly EqualityComparerFactory EqualityComparerFactory =
            new EqualityComparerFactory(
                new LinqToDbPropertyProvider(new MappingSchema(Schema.Erm, Schema.Facts, Schema.Aggregates, Schema.Messages)),
                new XDocumentComparer(),
                new DateTimeComparer());

        private readonly HashSetStore _store;

        public HashSetStoreFactory() => _store = new HashSetStore(EqualityComparerFactory);

        public IStore CreateStore() => _store;

        public IQuery CreateQuery() => _store;

        public void Dispose() { }
    }
}