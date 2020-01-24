using NuClear.Replication.Core.Tenancy;

namespace NuClear.ValidationRules.Storage.Model.Facts
{
    public sealed class EntityName : ITenantEntity
    {
        public long Id { get; set; }
        public int EntityType { get; set; }
        public Tenant TenantId { get; set; }
        public string Name { get; set; }
    }
}
