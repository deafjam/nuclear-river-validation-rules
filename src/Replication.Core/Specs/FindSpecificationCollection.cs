using System.Collections.Generic;

using NuClear.Storage.API.Specifications;

namespace NuClear.Replication.Core.Specs
{
    public sealed class FindSpecificationCollection<T> : FindSpecification<T>
    {
        public FindSpecificationCollection(IReadOnlyCollection<FindSpecification<T>> wrappedSpecs)
            : base(x => false)
        {
            WrappedSpecs = wrappedSpecs;
        }

        internal IReadOnlyCollection<FindSpecification<T>> WrappedSpecs { get; }
    }
}