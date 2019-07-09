using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Model.Messages;

using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using Ruleset = NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules.Ruleset;

namespace NuClear.ValidationRules.Replication.PriceRules.Aggregates
{
    public sealed class RulesetAggregateRootActor : AggregateRootActor<Ruleset>
    {
        public RulesetAggregateRootActor(
            IQuery query,
            IEqualityComparerFactory equalityComparerFactory,
            IBulkRepository<Ruleset> bulkRepository,
            IBulkRepository<Ruleset.AdvertisementAmountRestriction> advertisementAmountRestrictionBulkRepository)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new RulesetAccessor(query), bulkRepository,
                HasValueObject(new AdvertisementAmountRestrictionAccessor(query), advertisementAmountRestrictionBulkRepository));
        }

        public sealed class RulesetAccessor : DataChangesHandler<Ruleset>, IStorageBasedDataObjectAccessor<Ruleset>
        {
            private readonly IQuery _query;

            public RulesetAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator();

            public IQueryable<Ruleset> GetSource()
                => _query.For<Facts::Ruleset>().Select(x => new Ruleset { Id = x.Id });

            public FindSpecification<Ruleset> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
                return new FindSpecification<Ruleset>(x => aggregateIds.Contains(x.Id));
            }
        }

        public sealed class AdvertisementAmountRestrictionAccessor : DataChangesHandler<Ruleset.AdvertisementAmountRestriction>, IStorageBasedDataObjectAccessor<Ruleset.AdvertisementAmountRestriction>
        {
            private readonly IQuery _query;

            public AdvertisementAmountRestrictionAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        MessageTypeCode.AdvertisementAmountShouldMeetMaximumRestrictions,
                        MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictions,
                        MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictionsMass
                    };

            public IQueryable<Ruleset.AdvertisementAmountRestriction> GetSource()
            {
                var result =
                    from ruleset in _query.For(Specs.Find.Facts.Ruleset)
                    join project in _query.For<Facts::Ruleset.RulesetProject>() on ruleset.Id equals project.RulesetId
                    join rule in _query.For<Facts::Ruleset.QuantitativeRule>() on ruleset.Id equals rule.RulesetId
                    select new Ruleset.AdvertisementAmountRestriction
                    {
                        RulesetId = rule.RulesetId,
                        ProjectId = project.ProjectId,

                        Begin = ruleset.BeginDate,
                        End = ruleset.EndDate,

                        CategoryCode = rule.NomenclatureCategoryCode,

                        Max = rule.Max,
                        Min = rule.Min
                    };

                return result;
            }

            public FindSpecification<Ruleset.AdvertisementAmountRestriction> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Ruleset.AdvertisementAmountRestriction>(x => aggregateIds.Contains(x.RulesetId));
            }
        }
    }
}