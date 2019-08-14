using System;
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
    public sealed class PriceAccessor : IStorageBasedDataObjectAccessor<Price>, IDataChangesHandler<Price>
    {
        private readonly IQuery _query;

        public PriceAccessor(IQuery query) => _query = query;

        public IQueryable<Price> GetSource() => _query.For<Erm::Price>()
                                                      .Where(x => !x.IsDeleted && x.IsPublished)
                                                      .Select(x => new Price
                                                          {
                                                              Id = x.Id,
                                                              BeginDate = x.BeginDate,
                                                              ProjectId = x.ProjectId,
                                                          });

        public FindSpecification<Price> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<Price>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<Price> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<Price> dataObjects) => Array.Empty<IEvent>();
        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<Price> dataObjects) => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<Price> dataObjects)
        {
            var pricesIds = dataObjects.Select(x => x.Id).ToHashSet();

            var orderIds = (from price in _query.For<Price>().Where(x => pricesIds.Contains(x.Id))
                           from order in _query.For<Order>().Where(x => x.AgileDistributionStartDate >= price.BeginDate && x.ProjectId == price.ProjectId)
                           select order.Id)
                           .Distinct()
                           .ToList();

            return new IEvent[] { new RelatedDataObjectOutdatedEvent(typeof(Price), typeof(Order), orderIds) };
        }
    }
}