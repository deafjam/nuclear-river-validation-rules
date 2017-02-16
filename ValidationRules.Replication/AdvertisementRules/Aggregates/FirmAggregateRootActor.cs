﻿using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Storage.Model.AdvertisementRules.Aggregates;
using NuClear.ValidationRules.Storage.Model.Messages;

using Facts = NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.AdvertisementRules.Aggregates
{
    public sealed class FirmAggregateRootActor : AggregateRootActor<Firm>
    {
        public FirmAggregateRootActor(
            IQuery query,
            IEqualityComparerFactory equalityComparerFactory,
            IBulkRepository<Firm> bulkRepository,
            IBulkRepository<Firm.FirmWebsite> firmWebsiteBulkRepository,
            IBulkRepository<Firm.WhiteListDistributionPeriod> whiteListDistributionPeriodBulkRepository)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new FirmAccessor(query), bulkRepository,
                HasValueObject(new FirmWebsiteAccessor(query), firmWebsiteBulkRepository),
                HasValueObject(new WhiteListDistributionPeriodAccessor(query), whiteListDistributionPeriodBulkRepository));
        }

        public sealed class FirmAccessor : DataChangesHandler<Firm>, IStorageBasedDataObjectAccessor<Firm>
        {
            private readonly IQuery _query;

            public FirmAccessor(IQuery query) : base(CreateInvalidator())
            {
                _query = query;
            }

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        MessageTypeCode.AdvertisementMustBelongToFirm,
                        MessageTypeCode.AdvertisementWebsiteShouldNotBeFirmWebsite,
                        MessageTypeCode.WhiteListAdvertisementMayPresent,
                        MessageTypeCode.WhiteListAdvertisementMustPresent,
                    };

            public IQueryable<Firm> GetSource()
                => from firm in _query.For<Facts::Firm>()
                   select new Firm
                   {
                       Id = firm.Id,
                   };

            public FindSpecification<Firm> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<CreateDataObjectCommand>().Select(c => c.DataObjectId)
                                           .Concat(commands.OfType<SyncDataObjectCommand>().Select(c => c.DataObjectId))
                                           .Concat(commands.OfType<DeleteDataObjectCommand>().Select(c => c.DataObjectId))
                                           .Distinct()
                                           .ToArray();
                return new FindSpecification<Firm>(x => aggregateIds.Contains(x.Id));
            }
        }

        public sealed class FirmWebsiteAccessor : DataChangesHandler<Firm.FirmWebsite>, IStorageBasedDataObjectAccessor<Firm.FirmWebsite>
        {
            private readonly IQuery _query;

            public FirmWebsiteAccessor(IQuery query) : base(CreateInvalidator())
            {
                _query = query;
            }

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        MessageTypeCode.AdvertisementWebsiteShouldNotBeFirmWebsite,
                    };

            public IQueryable<Firm.FirmWebsite> GetSource() =>
                from firm in _query.For<Facts::Firm>().Where(x => x.IsActive && !x.IsDeleted && !x.IsClosedForAscertainment)
                from firmAddress in _query.For<Facts::FirmAddress>().Where(x => x.IsActive && !x.IsDeleted && !x.IsClosedForAscertainment).Where(x => x.FirmId == firm.Id)
                from firmAddressWebsite in _query.For<Facts::FirmAddressWebsite>().Where(x => x.FirmAddressId == firmAddress.Id)
                select new Firm.FirmWebsite
                    {
                        FirmId = firm.Id,
                        Website = firmAddressWebsite.Website
                    };

            public FindSpecification<Firm.FirmWebsite> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().Select(c => c.AggregateRootId).Distinct().ToArray();
                return new FindSpecification<Firm.FirmWebsite>(x => aggregateIds.Contains(x.FirmId));
            }
        }

        public sealed class WhiteListDistributionPeriodAccessor : DataChangesHandler<Firm.WhiteListDistributionPeriod>, IStorageBasedDataObjectAccessor<Firm.WhiteListDistributionPeriod>
        {
            private const int OrderOnRegistration = 1;

            private readonly IQuery _query;

            public WhiteListDistributionPeriodAccessor(IQuery query) : base(CreateInvalidator())
            {
                _query = query;
            }

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        MessageTypeCode.WhiteListAdvertisementMayPresent,
                        MessageTypeCode.WhiteListAdvertisementMustPresent,
                    };

            public IQueryable<Firm.WhiteListDistributionPeriod> GetSource()
            {
                var dates = _query.For<Facts::Order>().Select(x => new { Date = x.BeginDistribution, x.FirmId })
                                  .Union(_query.For<Facts::Order>().Select(x => new { Date = x.EndDistributionFact, x.FirmId }))
                                  .Union(_query.For<Facts::Order>().Select(x => new { Date = x.EndDistributionPlan, x.FirmId }));

                // todo: проверить, действительно ли тот баг проявляется здесь или можно убрать
                // https://github.com/linq2db/linq2db/issues/356
                dates = dates.Select(x => new { x.Date, x.FirmId });

                // todo: сравнить sql в этом варианте и с join
                var firmPeriods =
                    from date in dates
                    let nextDate = dates.Where(x => x.FirmId == date.FirmId && x.Date > date.Date).Min(x => (DateTime?)x.Date)
                    where nextDate.HasValue
                    select new { date.FirmId, Start = date.Date, End = nextDate.Value };

                var result =
                    from period in firmPeriods
                    let advertisementPosition =
                        (from order in _query.For<Facts::Order>().Where(x => x.FirmId == period.FirmId && x.BeginDistribution <= period.Start && period.End <= x.EndDistributionFact && x.WorkflowStep != OrderOnRegistration)
                         from op in _query.For<Facts::OrderPosition>().Where(x => x.OrderId == order.Id)
                         from opa in _query.For<Facts::OrderPositionAdvertisement>().Where(x => x.OrderPositionId == op.Id && x.AdvertisementId.HasValue)
                         from a in _query.For<Facts::Advertisement>().Where(x => x.Id == opa.AdvertisementId.Value && x.FirmId == period.FirmId && x.IsSelectedToWhiteList && ! x.IsDeleted)
                         select new { OrderId = order.Id, AdvertisementId = a.Id }).FirstOrDefault()
                    select new Firm.WhiteListDistributionPeriod
                    {
                        FirmId = period.FirmId,
                        Start = period.Start,
                        End = period.End,
                        AdvertisementId = advertisementPosition == null ? null : (long?)advertisementPosition.AdvertisementId,
                        ProvidedByOrderId = advertisementPosition == null ? null : (long?)advertisementPosition.OrderId,
                    };

                return result;
            }

            public FindSpecification<Firm.WhiteListDistributionPeriod> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().Select(c => c.AggregateRootId).Distinct().ToArray();
                return new FindSpecification<Firm.WhiteListDistributionPeriod>(x => aggregateIds.Contains(x.FirmId));
            }
        }
    }
}
