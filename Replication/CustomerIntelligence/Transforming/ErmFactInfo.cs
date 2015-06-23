using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming.Operations;
using NuClear.AdvancedSearch.Replication.Model;
using NuClear.Storage.Readings;

namespace NuClear.AdvancedSearch.Replication.CustomerIntelligence.Transforming
{
    internal abstract class ErmFactInfo
    {
        public static Builder<TFact> OfType<TFact>(params object[] x) where TFact : class, IErmFactObject, IIdentifiable
        {
            return new Builder<TFact>();
        }

        public abstract Type FactType { get; }

        public abstract IEnumerable<AggregateOperation> ApplyChangesWith(ErmFactsTransformation transformation, MergeTool.MergeResult<long> changes);

        public abstract MergeTool.MergeResult<long> DetectChangesWith(ErmFactsTransformation transformation, IReadOnlyCollection<long> factIds);

        internal class Builder<TFact> where TFact : class, IErmFactObject, IIdentifiable
        {
            private readonly List<FactDependencyInfo> _dependencies = new List<FactDependencyInfo>();
            private readonly Func<IQuery, IEnumerable<long>, IQueryable<TFact>> _factsQuery = (query, ids) => query.For<TFact>().Where(fact => ids.Contains(fact.Id));
            
            private Func<IQuery, IEnumerable<long>, IQueryable<TFact>> _ermQuery;
            
            public Builder<TFact> HasSource(Func<IQuery, IQueryable<TFact>> factQueryableProvider)
            {
                _ermQuery = (query, ids) => factQueryableProvider(query).Where(fact => ids.Contains(fact.Id));
                return this;
            }

            public Builder<TFact> HasDependentAggregate<TAggregate>(Func<IQuery, IEnumerable<long>, IEnumerable<long>> dependentAggregateIdsQueryProvider)
                where TAggregate : ICustomerIntelligenceObject
            {
                _dependencies.Add(FactDependencyInfo.Create<TAggregate>(dependentAggregateIdsQueryProvider));
                return this;
            }

            public Builder<TFact> HasMatchedAggregate<TAggregate>()
            {
                _dependencies.Add(FactDependencyInfo.Create<TAggregate>());
                return this;
            }

            public static implicit operator ErmFactInfo(Builder<TFact> builder)
            {
                return new ErmFactInfoImpl<TFact>(builder._ermQuery, builder._factsQuery, builder._dependencies);
            }
        }

        private class ErmFactInfoImpl<T> : ErmFactInfo
            where T : class, IErmFactObject
        {
            private readonly Func<IQuery, IEnumerable<long>, IQueryable<T>> _ermQuery;
            private readonly Func<IQuery, IEnumerable<long>, IQueryable<T>> _factsQuery;
            private readonly IReadOnlyCollection<FactDependencyInfo> _aggregates;

            public ErmFactInfoImpl(
                Func<IQuery, IEnumerable<long>, IQueryable<T>> ermQuery,
                Func<IQuery, IEnumerable<long>, IQueryable<T>> factsQuery,
                IReadOnlyCollection<FactDependencyInfo> aggregates)
            {
                _ermQuery = ermQuery;
                _factsQuery = factsQuery;
                _aggregates = aggregates ?? new FactDependencyInfo[0];
            }

            public override Type FactType
            {
                get { return typeof(T); }
            }

            public override MergeTool.MergeResult<long> DetectChangesWith(ErmFactsTransformation transformation, IReadOnlyCollection<long> factIds)
            {
                return transformation.DetectChanges(context => _factsQuery.Invoke(context, factIds));
            }

            public override IEnumerable<AggregateOperation> ApplyChangesWith(ErmFactsTransformation transformation, MergeTool.MergeResult<long> changes)
            {
                return transformation.ApplyChanges(_ermQuery, _aggregates, changes);
            }
        }
    }
}