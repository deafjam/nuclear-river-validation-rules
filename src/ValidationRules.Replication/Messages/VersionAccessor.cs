using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;

using UseCaseTrackingEvent = NuClear.ValidationRules.Storage.Model.Erm.UseCaseTrackingEvent;
using Version = NuClear.ValidationRules.Storage.Model.Messages.Version;

namespace NuClear.ValidationRules.Replication.Messages
{
    // stateinit-only accessor
    public sealed class VersionAccessor : IStorageBasedDataObjectAccessor<Version>
    {
        // ReSharper disable once UnusedParameter.Local
        public VersionAccessor(IQuery _) { }

        IQueryable<Version> IStorageBasedDataObjectAccessor<Version>.GetSource()
            => new[] { new Version { Id = 0, UtcDateTime = DateTime.UtcNow } }.AsQueryable();

        FindSpecification<Version> IStorageBasedDataObjectAccessor<Version>.GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            => throw new NotSupportedException();
    }

    // stateinit-only accessor
    public sealed partial class ErmStateAccessor : IStorageBasedDataObjectAccessor<Version.ErmState>
    {
        private readonly IQuery _query;

        public ErmStateAccessor(IQuery query) => _query = query;

        IQueryable<Version.ErmState> IStorageBasedDataObjectAccessor<Version.ErmState>.GetSource() =>
            _query.For<UseCaseTrackingEvent>()
                .Where(x => x.EventType == UseCaseTrackingEvent.Committed)
                .OrderByDescending(x => x.CreatedOn)
                .Take(1)
                .Select(x => new Version.ErmState
                {
                    VersionId = 0,
                    Token = x.UseCaseId,
                    UtcDateTime = x.CreatedOn
                });

        FindSpecification<Version.ErmState> IStorageBasedDataObjectAccessor<Version.ErmState>.GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            => throw new NotSupportedException();
    }
}
