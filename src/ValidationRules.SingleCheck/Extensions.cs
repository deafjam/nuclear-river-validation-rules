using System;
using System.Collections.Generic;
using System.Linq;

namespace NuClear.ValidationRules.SingleCheck
{
    public static class Extensions
    {
        public static IReadOnlyCollection<T> Execute<T>(this IQueryable<T> queryable)
        {
            try
            {
                return queryable.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception while querying for {typeof(T).FullName}\n{queryable}", ex);
            }
        }
    }
}
