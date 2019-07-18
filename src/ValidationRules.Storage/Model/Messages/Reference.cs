using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Model.Common;
using NuClear.Model.Common.Entities;

namespace NuClear.ValidationRules.Storage.Model.Messages
{
    public class Reference
    {
        public static IEqualityComparer<Reference> Comparer = new ReferenceComparer();

        public Reference(int entityTypeId, long id)
            : this(entityTypeId, id, null)
        {
        }

        public Reference(int entityTypeId, long id, params Reference[] children)
        {
            Children = children ?? Array.Empty<Reference>();
            EntityType = entityTypeId;
            Id = id;
        }

        public int EntityType { get; }
        public long Id { get; }
        public IReadOnlyCollection<Reference> Children { get; }

        private class ReferenceComparer : IEqualityComparer<Reference>
        {
            public bool Equals(Reference x, Reference y)
            {
                return
                    x.EntityType == y.EntityType &&
                    x.Id == y.Id &&
                    x.Children.Count == y.Children.Count &&
                    x.Children.SequenceEqual(y.Children, this);
            }

            public int GetHashCode(Reference obj)
            {
                unchecked
                {
                    var hashCode = obj.EntityType;
                    hashCode = (hashCode * 397) ^ obj.Id.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Children.Aggregate(0, (accum, reference) => (accum * 397) ^ GetHashCode(reference));
                    return hashCode;
                }
            }
        }
    }

    public class Reference<TEntityType> : Reference
        where TEntityType : IdentityBase<TEntityType>, new()
    {
        private static readonly int EntityTypeId = EntityTypeBase<TEntityType>.Instance.Id;

        public Reference(long id)
            : base(EntityTypeId, id)
        {
        }

        public Reference(long id, params Reference[] children)
            : base(EntityTypeId, id, children)
        {
        }
    }
}
