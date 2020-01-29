namespace NuClear.Replication.Core.Tenancy
{
    // todo: возможно, пригодится.
    //public class TenantSpecificQuery : IQuery
    //{
    //    private readonly IQuery _implementation;
    //    private readonly Tenant _tenant;

    //    public TenantSpecificQuery(IQuery implementation, Tenant tenant)
    //    {
    //        _implementation = implementation;
    //        _tenant = tenant;
    //    }

    //    public IQueryable For(Type objType)
    //        => throw new NotSupportedException();

    //    public IQueryable<T> For<T>() where T : class
    //        => typeof(ITenantEntity).IsAssignableFrom(typeof(T))
    //            ? _implementation.For<T>().Select(TenantExtensions.ApplyToEntity<T>(_tenant))
    //            : _implementation.For<T>();

    //    public IQueryable<T> For<T>(FindSpecification<T> findSpecification) where T : class
    //        => typeof(ITenantEntity).IsAssignableFrom(typeof(T))
    //            ? _implementation.For(findSpecification).Select(TenantExtensions.ApplyToEntity<T>(_tenant))
    //            : _implementation.For(findSpecification);
    //}
}