using NuClear.Replication.Core.Tenancy;

namespace NuClear.ValidationRules.SingleCheck.Tenancy
{
    public interface ITenantProvider
    {
        Tenant Current { get; }
    }
}
