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
    public sealed class LegalPersonAccessor : IStorageBasedDataObjectAccessor<LegalPerson>, IDataChangesHandler<LegalPerson>
    {
        private readonly IQuery _query;

        public LegalPersonAccessor(IQuery query) => _query = query;

        public IQueryable<LegalPerson> GetSource() => _query
            .For<Erm::LegalPerson>()
            .Where(x => x.IsActive && !x.IsDeleted)
            .Select(x => new LegalPerson
                {
                    Id = x.Id
                });

        public FindSpecification<LegalPerson> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
        {
            var ids = commands.Cast<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
            return SpecificationFactory<LegalPerson>.Contains(x => x.Id, ids);
        }

        public IReadOnlyCollection<IEvent> HandleCreates(IReadOnlyCollection<LegalPerson> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleUpdates(IReadOnlyCollection<LegalPerson> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleDeletes(IReadOnlyCollection<LegalPerson> dataObjects)
            => Array.Empty<IEvent>();

        public IReadOnlyCollection<IEvent> HandleRelates(IReadOnlyCollection<LegalPerson> dataObjects)
        {
            var legalPersonIds = dataObjects.Select(x => x.Id).ToHashSet();

            var orderIds = _query.For<OrderConsistency>()
                .Where(x => legalPersonIds.Contains(x.LegalPersonId.Value))
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            return new[] {new RelatedDataObjectOutdatedEvent(typeof(LegalPerson), typeof(Order), orderIds)};
        }
    }
}