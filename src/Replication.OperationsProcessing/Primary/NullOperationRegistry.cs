using NuClear.Model.Common.Entities;
using NuClear.Model.Common.Operations.Identity;

namespace NuClear.Replication.OperationsProcessing.Primary
{
    public sealed class NullOperationRegistry<TSubDomain> : IOperationRegistry<TSubDomain>
        where TSubDomain : ISubDomain
    {
        public bool IsAllowedOperation(StrictOperationIdentity operationIdentity)
            => true;

        public bool IsIgnoredOperation(StrictOperationIdentity operationIdentity)
            => false;
    }
}