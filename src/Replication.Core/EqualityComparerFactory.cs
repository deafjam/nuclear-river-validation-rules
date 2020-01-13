using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using NuClear.Replication.Core.Equality;

namespace NuClear.Replication.Core
{
    public sealed class EqualityComparerFactory : IEqualityComparerFactory
    {
        private readonly IObjectPropertyProvider _propertyProvider;
        private readonly IDictionary<Type, EqualityComparerProxy> _equalityComparers;
        private readonly IDictionary<Type, object> _identityComparerCache;
        private readonly IDictionary<Type, object> _completeComparerCache;

        public EqualityComparerFactory(IObjectPropertyProvider propertyProvider)
            : this(propertyProvider, null)
        {
        }

        /// <summary>
        /// Инициализирует экземпляр
        /// </summary>
        /// <param name="propertyProvider">Используется для получения свойств сущностей, участвующих в сравнении</param>
        /// <param name="equalityComparers">Опциональные пользовательские IEqualityComparer<T> для нестандартного поведения при сравнения полей сущностей</param>
        public EqualityComparerFactory(IObjectPropertyProvider propertyProvider, params object[] equalityComparers)
        {
            _propertyProvider = propertyProvider;
            _equalityComparers = CreateComparersDictionary(equalityComparers ?? Array.Empty<object>());
            _identityComparerCache = new Dictionary<Type, object>();
            _completeComparerCache = new Dictionary<Type, object>();
        }

        private static IDictionary<Type, EqualityComparerProxy> CreateComparersDictionary(IEnumerable<object> equalityComparers)
        {
            var proxies =
                from comparer in equalityComparers
                from interfaceType in comparer.GetType().GetInterfaces()
                where interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEqualityComparer<>)
                select new EqualityComparerProxy(interfaceType, comparer);

            return proxies.ToDictionary(x => x.ValueType, x => x);
        }

        public IEqualityComparer<T> CreateIdentityComparer<T>()
        {
            object comparer;
            if (!_identityComparerCache.TryGetValue(typeof(T), out comparer))
            {
                var properties = _propertyProvider.GetPrimaryKeyProperties<T>();
                var equality = CreateEqualityFunction<T>(properties);
                var hashCode = CreateHashCodeFunction<T>(properties);
                _identityComparerCache[typeof(T)] = comparer = new EqualityComparerWrapper<T>(equality, hashCode);
            }

            return (IEqualityComparer<T>)comparer;
        }

        public IEqualityComparer<T> CreateCompleteComparer<T>()
        {
            object comparer;
            if (!_completeComparerCache.TryGetValue(typeof(T), out comparer))
            {
                var properties = _propertyProvider.GetProperties<T>();
                var equality = CreateEqualityFunction<T>(properties);
                var hashCode = CreateHashCodeFunction<T>(properties);
                _completeComparerCache[typeof(T)] = comparer = new EqualityComparerWrapper<T>(equality, hashCode);
            }

            return (IEqualityComparer<T>)comparer;
        }

        private Func<T, int> CreateHashCodeFunction<T>(IReadOnlyCollection<PropertyInfo> properties)
        {
            // T x => ((((x.P1.GetHashCode()) * 397) ^ x.P2.GetHashCode()) * 397) ^ x.P3.GetHashCode()

            var parameter = Expression.Parameter(typeof(T));
            var constPrimeNumber = Expression.Constant(397);

            var hashCodeMethod = typeof(object).GetRuntimeMethod("GetHashCode", new Type[0]);
            var hashCode = properties.Aggregate(
                (Expression)Expression.Constant(0),
                (acc, property) =>
                {
                    var propertyAccess = Expression.Property((Expression)parameter, (PropertyInfo)property);
                    EqualityComparerProxy comparer;
                    var hashCodeCall =
                        _equalityComparers.TryGetValue(property.PropertyType, out comparer)
                            ? comparer.CreateHashCodeCall(propertyAccess)
                            : Expression.Call(propertyAccess, hashCodeMethod);

                    if (!property.PropertyType.IsValueType)
                    {
                        hashCodeCall = Expression.Condition(
                            Expression.ReferenceNotEqual(propertyAccess, Expression.Constant(null, property.PropertyType)),
                            hashCodeCall,
                            Expression.Constant(0, typeof(int)));
                    }

                    return Expression.ExclusiveOr(Expression.Multiply(acc, constPrimeNumber), hashCodeCall);
                });

            return Expression.Lambda<Func<T, int>>(hashCode, parameter).Compile();
        }

        private Func<T, T, bool> CreateEqualityFunction<T>(IReadOnlyCollection<PropertyInfo> properties)
        {
            // (T x, T y) => x.Property1 == y.Property1 && x.Property2 == y.Property2 && ... && x.PropertyN == y.PropertyN

            var left = Expression.Parameter(typeof(T));
            var right = Expression.Parameter(typeof(T));
            var compare = properties.Select(p => CreateEqualityExpression(p, left, right))
                                    .Aggregate((Expression)Expression.Constant(true), Expression.And);

            return Expression.Lambda<Func<T, T, bool>>(compare, left, right).Compile();
        }

        private Expression CreateEqualityExpression(PropertyInfo property, ParameterExpression left, ParameterExpression right)
        {
            EqualityComparerProxy comparer;
            return _equalityComparers.TryGetValue(property.PropertyType, out comparer)
                       ? comparer.CreateEqualsCall(Expression.Property(left, property), Expression.Property(right, property))
                       : Expression.Equal(Expression.Property(left, property), Expression.Property(right, property));
        }

        private sealed class EqualityComparerProxy
        {
            private readonly object _comparerInstance;
            private readonly MethodInfo _getHashCodeMethod;
            private readonly MethodInfo _equalsMethod;

            public EqualityComparerProxy(Type interfaceType, object comparerInstance)
            {
                ValueType = interfaceType.GetGenericArguments().Single();

                _comparerInstance = comparerInstance;
                _getHashCodeMethod = interfaceType.GetMethod("GetHashCode", BindingFlags.Instance | BindingFlags.Public);
                _equalsMethod = interfaceType.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public);
            }

            public Type ValueType { get; }

            public Expression CreateHashCodeCall(Expression value)
            {
                return Expression.Call(Expression.Constant(_comparerInstance), _getHashCodeMethod, value);
            }

            public Expression CreateEqualsCall(Expression left, Expression right)
            {
                return Expression.Call(Expression.Constant(_comparerInstance), _equalsMethod, left, right);
            }
        }
    }
}
