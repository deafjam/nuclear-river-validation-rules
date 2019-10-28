using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using NuClear.Storage.API.Specifications;

namespace NuClear.Replication.Core.Specs
{
    public static class SpecificationFactory<TDataObject>
    {
        private const int MsSqlInExpressionLimit = 5000;

        public static FindSpecificationCollection<TDataObject> Contains<TKey>(
            Expression<Func<TDataObject, TKey>> keyProperyExpression,
            IReadOnlyCollection<TKey> keys)
        {
            return Contains(keyProperyExpression, keys, MsSqlInExpressionLimit);
        }

        public static FindSpecificationCollection<TDataObject> Contains<TKey>(
            Expression<Func<TDataObject, TKey>> keyProperyExpression,
            IReadOnlyCollection<TKey> keys,
            int limit)
        {
            var specs = Enumerable.Range(0, (limit + keys.Count - 1) / limit)
                                  .Select(x =>
                                      {
                                          var batch = keys.Skip(x * limit).Take(limit);
                                          var expression = Containment<TKey>.Create(keyProperyExpression, batch);
                                          return new FindSpecification<TDataObject>(expression);
                                      })
                                  .ToArray();

            return new FindSpecificationCollection<TDataObject>(specs);
        }

        private static class Containment<TKey>
        {
            private static readonly Func<IEnumerable<TKey>, TKey, bool> Contains = Enumerable.Contains;

            public static Expression<Func<TDataObject, bool>> Create(Expression<Func<TDataObject, TKey>> keyProperty, IEnumerable<TKey> batch)
            {
                // x => batch.Contains(x.KeyProperty)
                var containsCall = Expression.Call(null, Contains.Method, Expression.Constant(batch), keyProperty.Body);
                var expression = Expression.Lambda<Func<TDataObject, bool>>(containsCall, keyProperty.Parameters[0]);
                return expression;
            }
        }
    }
}