using System.Collections.Generic;

using NuClear.Storage.API.Specifications;

namespace NuClear.Replication.Core.DataObjects
{
    public interface IMemoryBasedDataObjectAccessor<TDataObject>
    {
        IReadOnlyCollection<TDataObject> GetDataObjects(IEnumerable<ICommand> commands);
        FindSpecification<TDataObject> GetFindSpecification(IEnumerable<ICommand> commands);
    }
}