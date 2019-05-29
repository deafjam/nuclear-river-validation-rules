using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.Telemetry.Probing;

namespace NuClear.ValidationRules.SingleCheck.Store
{
    /// <summary>
    /// Обеспечивает хранение в постоянных sql-таблицах с откатом всех изменений по завершению проверки.
    /// </summary>
    public sealed class PersistentTableStoreFactory : IStoreFactory
    {
        private readonly IEqualityComparerFactory _equalityComparerFactory;
        private readonly DataConnection _connection;

        public PersistentTableStoreFactory(IEqualityComparerFactory equalityComparerFactory, MappingSchema webAppMappingSchema)
        {
            _equalityComparerFactory = equalityComparerFactory;

            using (Probe.Create("Get lock"))
            {
                _connection = new DataConnection("ValidationRules").AddMappingSchema(webAppMappingSchema);

                // чтобы параллельные запуски single-проверок не накладывали блокировки на webapp-таблицы и не ждали друг друга
                // запускаем single-проверки в транзакции с режимом snapshot, который не накладывает никаких блокировок
                _connection.BeginTransaction(System.Data.IsolationLevel.Snapshot);
            }
        }

        public IStore CreateStore() => new Linq2DbStore(_equalityComparerFactory, _connection);

        public IQuery CreateQuery() => new Linq2DbQuery(_connection);

        public void Dispose()
        {
            // не коммитим транзакцию
            _connection.RollbackTransaction();
            _connection.Dispose();
        }

        private sealed class Linq2DbQuery : IQuery
        {
            private readonly DataConnection _connection;

            public Linq2DbQuery(DataConnection connection) => _connection = connection;

            IQueryable<T> IQuery.For<T>() => _connection.GetTable<T>();

            IQueryable IQuery.For(Type objType) => throw new NotSupportedException();

            IQueryable<T> IQuery.For<T>(FindSpecification<T> findSpecification) => _connection.GetTable<T>().Where(findSpecification);
        }

        private sealed class Linq2DbStore : IStore
        {
            private readonly IEqualityComparerFactory _equalityComparerFactory;
            private readonly DataConnection _connection;

            public Linq2DbStore(IEqualityComparerFactory equalityComparerFactory, DataConnection connection) =>
                (_equalityComparerFactory, _connection) = (equalityComparerFactory, connection);

            void IStore.Add<T>(T entity) => _connection.Insert(entity);

            void IStore.AddRange<T>(IReadOnlyCollection<T> entities) =>
                _connection.GetTable<T>().BulkCopy(new HashSet<T>(entities, _equalityComparerFactory.CreateIdentityComparer<T>()));
        }
    }
}