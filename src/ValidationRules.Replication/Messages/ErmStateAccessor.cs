using System;
using System.Collections.Generic;
using System.Linq;
using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using Version = NuClear.ValidationRules.Storage.Model.Messages.Version;

namespace NuClear.ValidationRules.Replication.Messages
{
    public sealed partial class ErmStateAccessor: IMemoryBasedDataObjectAccessor<Version.ErmState>, IDataChangesHandler<Version.ErmState>
    {
        public IReadOnlyCollection<Version.ErmState> GetDataObjects(ICommand command)
        {
            var dtos = ((ReplaceDataObjectCommand)command).Dtos.Cast<Version.ErmState>();
            
            return dtos.ToList();
        }

        public FindSpecification<Version.ErmState> GetFindSpecification(ICommand command)
        {
            var dtos = ((ReplaceDataObjectCommand)command).Dtos.Cast<Version.ErmState>();
            var ids = dtos.Select(x => x.Token);

            return new FindSpecification<Version.ErmState>(x => ids.Contains(x.Token));
        }

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<Version.ErmState> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<Version.ErmState> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<Version.ErmState> dataObjects) => throw new NotSupportedException();
        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<Version.ErmState> dataObjects) => throw new NotSupportedException();
    }
}