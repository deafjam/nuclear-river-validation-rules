using NuClear.Model.Common.Entities;
using NuClear.ValidationRules.Replication;

namespace NuClear.ValidationRules.OperationsProcessing.Identities.EntityTypes
{
    public sealed class EntityTypePosition : EntityTypeBase<EntityTypePosition>
    {
        public override int Id { get; } = EntityTypeIds.Position;
        public override string Description { get; } = nameof(EntityTypeIds.Position);
    }
}