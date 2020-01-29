using NuClear.Storage.API.ConnectionStrings;

namespace NuClear.Replication.Core.Tenancy
{
    public interface ITenantConnectionStringSettings : IConnectionStringSettings
    {
        string GetConnectionString(IConnectionStringIdentity identity, Tenant tenant);
    }
}