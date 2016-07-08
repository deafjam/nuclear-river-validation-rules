﻿using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Events;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Model.AccountRules.Facts;

namespace NuClear.ValidationRules.Replication.AccountRules.Facts
{
    public sealed class ProjectAccessor : IStorageBasedDataObjectAccessor<Project>, IDataChangesHandler<Project>
    {
        private readonly IQuery _query;

        public ProjectAccessor(IQuery query)
        {
            _query = query;
        }

        public IQueryable<Project> GetSource()
            => _query.For(Specs.Find.Erm.Projects()).Select(x => new Project { Id = x.Id, OrganizationUnitId = x.OrganizationUnitId.Value });

        public FindSpecification<Project> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().Select(c => c.DataObjectId).ToArray();
            return new FindSpecification<Project>(x => ids.Contains(x.Id));
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<Project> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<Project> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<Project> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<Project> dataObjects)
        {
            var ids = dataObjects.Select(x => x.OrganizationUnitId).ToArray();
            var orderIds = _query.For<Order>().Where(x => ids.Contains(x.DestOrganizationUnitId)).Select(x => x.Id).ToArray();
            return orderIds.Select(x => new RelatedDataObjectOutdatedEvent<long>(typeof(Order), x)).ToArray();
        }
    }
}