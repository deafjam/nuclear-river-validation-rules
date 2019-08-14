﻿using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Specs;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Events;
using NuClear.ValidationRules.Storage.Model.Facts;

using Erm = NuClear.ValidationRules.Storage.Model.Erm;

namespace NuClear.ValidationRules.Replication.Accessors
{
    public sealed class ThemeOrganizationUnitAccessor : IStorageBasedDataObjectAccessor<ThemeOrganizationUnit>, IDataChangesHandler<ThemeOrganizationUnit>
    {
        private readonly IQuery _query;

        public ThemeOrganizationUnitAccessor(IQuery query) => _query = query;

        public IQueryable<ThemeOrganizationUnit> GetSource() => _query
            .For<Erm::ThemeOrganizationUnit>()
            .Where(x => x.IsActive && !x.IsDeleted)
            .Select(x => new ThemeOrganizationUnit
            {
                Id = x.Id,
                ThemeId = x.ThemeId,
                OrganizationUnitId = x.OrganizationUnitId,
            });

        public FindSpecification<ThemeOrganizationUnit> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<ThemeOrganizationUnit>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<ThemeOrganizationUnit> dataObjects) => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<ThemeOrganizationUnit> dataObjects) => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<ThemeOrganizationUnit> dataObjects) => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<ThemeOrganizationUnit> dataObjects)
        {
            var organizationUnitIds = dataObjects.Select(x => x.OrganizationUnitId).ToHashSet();

            var projectIds = _query.For<Project>()
                .Where(x => organizationUnitIds.Contains(x.OrganizationUnitId))
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(ThemeOrganizationUnit), typeof(Project), projectIds)};
        }
    }
}