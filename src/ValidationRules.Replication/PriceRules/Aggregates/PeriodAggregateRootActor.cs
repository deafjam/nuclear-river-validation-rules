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

            public PeriodAccessor(IQuery query) : base(CreateInvalidator(x => GetRelatedOrders(query, x))) => _query = query;

            private static IRuleInvalidator CreateInvalidator(Func<IReadOnlyCollection<Period>, IEnumerable<long>> func)
                => new RuleInvalidator
                {
                    {MessageTypeCode.AdvertisementCountPerCategoryShouldBeLimited, func},
                    {MessageTypeCode.AdvertisementCountPerThemeShouldBeLimited, func},
                    {MessageTypeCode.AdvertisementAmountShouldMeetMaximumRestrictions, func},
                    {MessageTypeCode.AdvertisementAmountShouldMeetMinimumRestrictionsMass, func},
                    {MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions, func},
                };

            private static IEnumerable<long> GetRelatedOrders(IQuery query, IReadOnlyCollection<Period> dataObjects)
            {
                return query.For(PeriodSpec(dataObjects)).Select(x => x.Id);
                
                FindSpecification<Order> PeriodSpec(IEnumerable<Period> periods)
                {
                    var projectSpecs = periods.GroupBy(x => x.ProjectId).Select(x =>
                    {
                        var timeRanges = x.Select(y => new TimeRange(y.Start, y.End)).Merge();
                        var timeRangesSpec = timeRanges.Select(TimeRangeSpec).Aggregate(new FindSpecification<Order>(y => false), (current, spec) => current | spec);
                    
                        return new FindSpecification<Order>(y => y.ProjectId == x.Key) & timeRangesSpec;
                    });
                
                    return projectSpecs.Aggregate(new FindSpecification<Order>(x => false), (current, spec) => current | spec);
                    
                    FindSpecification<Order> TimeRangeSpec(TimeRange timeRange) => new FindSpecification<Order>(x => x.Start <= timeRange.End && timeRange.Start <= x.End);
                }
            }
            
            public IQueryable<Period> GetSource()
            {
                var dates =
                    _query.For<Facts::Order>().Select(x => new {x.ProjectId, Date = x.AgileDistributionStartDate})
                        .Union(_query.For<Facts::Order>().Select(x => new {x.ProjectId, Date = x.AgileDistributionEndFactDate}))
                        .Union(_query.For<Facts::Order>().Select(x => new {x.ProjectId, Date = x.AgileDistributionEndPlanDate}))
                        .Union(_query.For<Facts::Order>().Select(x => new {x.ProjectId, Date = DateTime.MinValue}));
               
                var result =
                    from date in dates
                    from next in dates.Where(x => x.ProjectId == date.ProjectId && x.Date > date.Date).OrderBy(x => x.Date).Take(1).DefaultIfEmpty()
                    select new Period
                    {
                        ProjectId = date.ProjectId,
                        Start = date.Date,
                        End = next != null ? next.Date : DateTime.MaxValue
                    };

                return result;
            }

            public FindSpecification<Period> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
                return new FindSpecification<Period>(x => aggregateIds.Contains(x.ProjectId));
            }
        }
    }
}