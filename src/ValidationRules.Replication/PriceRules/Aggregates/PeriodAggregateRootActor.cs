using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.PriceRules.Aggregates
{
    public sealed class PeriodAggregateRootActor : AggregateRootActor<Period>
    {
        public PeriodAggregateRootActor(
            IQuery query,
            IEqualityComparerFactory equalityComparerFactory,
            IBulkRepository<Period> bulkRepository)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new PeriodAccessor(query), bulkRepository);
        }

        public sealed class PeriodAccessor : DataChangesHandler<Period>, IStorageBasedDataObjectAccessor<Period>
        {
            private readonly IQuery _query;

            public PeriodAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        MessageTypeCode.AdvertisementCountPerCategoryShouldBeLimited,
                        MessageTypeCode.AdvertisementCountPerThemeShouldBeLimited,
                        MessageTypeCode.AdvertisementAmountShouldMeetMaximumRestrictions,
                        MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictionsMass,
                        MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions,
                    };

            public IQueryable<Period> GetSource()
            {
                var orderDatesOrgUnit =
                    _query.For<Facts::Order>().Select(x => new {x.DestOrganizationUnitId, Date = x.AgileDistributionStartDate})
                        .Union(_query.For<Facts::Order>().Select(x => new {x.DestOrganizationUnitId, Date = x.AgileDistributionEndFactDate}))
                        .Union(_query.For<Facts::Order>().Select(x => new {x.DestOrganizationUnitId, Date = x.AgileDistributionEndPlanDate}));

                var orderDates =
                    from orderDateOrgUnit in orderDatesOrgUnit
                    from project in _query.For<Facts::Project>().Where(x => x.OrganizationUnitId == orderDateOrgUnit.DestOrganizationUnitId)
                    select new { ProjectId = project.Id, orderDateOrgUnit.Date};
                
                var priceDates = _query.For<Facts::Price>().Select(x => new {x.ProjectId, Date = x.BeginDate});

                var dates = orderDates.Union(priceDates);

                var result =
                    from date in dates
                    from next in dates.Where(x => x.ProjectId == date.ProjectId && x.Date > date.Date).OrderBy(x => x.Date).Take(1).DefaultIfEmpty()
                    select new Period { ProjectId = date.ProjectId, Start = date.Date, End = next != null ? next.Date : DateTime.MaxValue };

                return result;
            }

            public FindSpecification<Period> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var ids = commands.Cast<SyncPeriodCommand>()
                    .SelectMany(x => x.PeriodKeys)
                    .ToHashSet();

                return Periods(ids);
            }

            private static FindSpecification<Period> Periods(IEnumerable<PeriodKey> periodKeys)
            {
                return periodKeys.Select(PeriodSpecificationForPeriodKey)
                                   .Aggregate(new FindSpecification<Period>(x => false), (current, spec) => current | spec);
            }

            private static FindSpecification<Period> PeriodSpecificationForPeriodKey(PeriodKey periodKey)
                => new FindSpecification<Period>(x => x.ProjectId == periodKey.ProjectId && x.Start <= periodKey.Date && periodKey.Date <= x.End);

        }
    }
}