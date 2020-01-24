using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace NuClear.Replication.Core.Tenancy
{
    public static class TenantExtensions
    {
        // Строит выражение для sql-запроса с подстановкой TenantId.
        // Что-то типа (X x) => new X { Id = x.Id, ..., TenantId = const }
        public static Expression<Func<T, T>> ApplyToEntity<T>(this Tenant tenant)
            => (Expression<Func<T, T>>)CreateExpressionWithTenant(tenant, typeof(T));

        private static LambdaExpression CreateExpressionWithTenant(Tenant tenant, Type entityType)
        {
            var parameter = Expression.Parameter(entityType);
            return Expression.Lambda(BuildBody(parameter, tenant), parameter);
        }

        private static MemberInitExpression BuildBody(ParameterExpression parameter, Tenant tenant)
        {
            var defaultEntityCtor = parameter.Type.GetConstructor(Array.Empty<Type>());
            if (defaultEntityCtor == null)
                throw new ArgumentException($"Type {parameter.Type} must have public parameterless constructor.");

            return Expression.MemberInit(Expression.New(defaultEntityCtor), BuildPropertyInit(parameter, tenant));
        }

        private static IEnumerable<MemberBinding> BuildPropertyInit(ParameterExpression parameter, Tenant tenant)
            => parameter.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => BuildPropertyAssign(parameter, property, tenant));

        private static MemberBinding BuildPropertyAssign(ParameterExpression parameter, PropertyInfo property,
            Tenant tenant)
            => string.Equals(property.Name, nameof(ITenantEntity.TenantId))
                ? BuildTenantPropertyAssign(property, tenant)
                : BuildPropertyAssign(parameter, property);

        private static MemberBinding BuildTenantPropertyAssign(PropertyInfo property, Tenant tenant)
            => Expression.Bind(property, Expression.Constant(tenant));

        private static MemberBinding BuildPropertyAssign(ParameterExpression parameter, PropertyInfo property)
            => Expression.Bind(property, Expression.Property(parameter, property));
    }
}