using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core.Specs;
using NuClear.Storage.API.Specifications;

namespace NuClear.Replication.Core
{
    public static class LinqExtensions
    {
        public static IEnumerable<IReadOnlyCollection<T>> CreateBatches<T>(this IEnumerable<T> items, int batchSize)
        {
            var buffer = new List<T>(batchSize);

            foreach (var item in items)
            {
                buffer.Add(item);

                if (buffer.Count == buffer.Capacity)
                {
                    yield return buffer;
                    buffer = new List<T>(batchSize);
                }
            }

            if (buffer.Count > 0)
            {
                yield return buffer;
            }
        }

        public static IEnumerable<T> WhereMatched<T>(this IQueryable<T> queryable, FindSpecification<T> specification)
        {
            var spec = specification as FindSpecificationCollection<T>;
            return spec?.WrappedSpecs.SelectMany(queryable.Where) ?? queryable.Where(specification);
        }
    }
}