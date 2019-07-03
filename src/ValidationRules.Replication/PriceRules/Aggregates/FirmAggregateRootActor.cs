using System;
using System.Collections.Generic;
using System.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.Replication.Core.Equality;
using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;
using NuClear.ValidationRules.Replication.Commands;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;
using NuClear.ValidationRules.Storage.Model.Messages;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.PriceRules.Aggregates
{
    public sealed class FirmAggregateRootActor : AggregateRootActor<Firm>
    {
        public FirmAggregateRootActor(
            IQuery query,
            IEqualityComparerFactory equalityComparerFactory,
            IBulkRepository<Firm> firmRepository,
            IBulkRepository<Firm.FirmPosition> firmPositionRepository,
            IBulkRepository<Firm.FirmAssociatedPosition> firmAssociatedPositionRepository,
            IBulkRepository<Firm.FirmDeniedPosition> firmDeniedPositionRepository)
            : base(query, equalityComparerFactory)
        {
            HasRootEntity(new FirmAccessor(query), firmRepository,
                HasValueObject(new FirmPositionAccessor(query), firmPositionRepository),
                HasValueObject(new FirmAssociatedPositionAccessor(query), firmAssociatedPositionRepository),
                HasValueObject(new FirmDeniedPositionAccessor(query), firmDeniedPositionRepository));
        }

        public sealed class FirmAccessor : DataChangesHandler<Firm>, IStorageBasedDataObjectAccessor<Firm>
        {
            private readonly IQuery _query;

            public FirmAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator();

            public IQueryable<Firm> GetSource()
                => _query.For<Facts.Order>().Select(order => new Firm { Id = order.FirmId }).Distinct();

            public FindSpecification<Firm> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.OfType<SyncDataObjectCommand>().SelectMany(c => c.DataObjectIds).ToHashSet();
                return new FindSpecification<Firm>(x => aggregateIds.Contains(x.Id));
            }
        }

        public sealed class FirmPositionAccessor : DataChangesHandler<Firm.FirmPosition>, IStorageBasedDataObjectAccessor<Firm.FirmPosition>
        {
            private readonly IQuery _query;

            public FirmPositionAccessor(IQuery query) : base(CreateInvalidator()) => _query = query;

            private static IRuleInvalidator CreateInvalidator()
                => new RuleInvalidator
                    {
                        { MessageTypeCode.FirmAssociatedPositionMustHavePrincipal, GetRelatedOrders },
                        { MessageTypeCode.FirmAssociatedPositionMustHavePrincipalWithDifferentBindingObject, GetRelatedOrders },
                        { MessageTypeCode.FirmPositionMustNotHaveDeniedPositions, GetRelatedOrders },
                        { MessageTypeCode.FirmAssociatedPositionMustHavePrincipalWithMatchedBindingObject, GetRelatedOrders },
                    };

            private static IEnumerable<long> GetRelatedOrders(IReadOnlyCollection<Firm.FirmPosition> dataObjects) =>
                dataObjects.Select(x => x.OrderId);

            public IQueryable<Firm.FirmPosition> GetSource()
            {
                var dates =
                    _query.For<Facts::Order>().Select(x => new { x.FirmId, Date = x.AgileDistributionStartDate })
                          .Union(_query.For<Facts::Order>().Select(x => new { x.FirmId, Date = x.AgileDistributionEndPlanDate }))
                          .Union(_query.For<Facts::Order>().Select(x => new { x.FirmId, Date = x.AgileDistributionEndFactDate }))
                          .Distinct();

                var firmPeriods =
                    from begin in dates
                    let end = dates.Where(x => x.FirmId == begin.FirmId && x.Date > begin.Date).Min(x => (DateTime?)x.Date)
                    where end.HasValue
                    select new { begin.FirmId, Begin = begin.Date, End = end.Value };

                var principals =
                    from order in _query.For<Facts::Order>()
                    from orderItem in _query.For<Facts::OrderItem>().Where(x => order.Id == x.OrderId)
                    from firmPeriod in firmPeriods.Where(x => x.FirmId == order.FirmId && x.Begin >= order.AgileDistributionStartDate && x.End <= order.AgileDistributionEndPlanDate)
                    from category in _query.For<Facts::Category>().Where(x => x.Id == orderItem.CategoryId).DefaultIfEmpty()
                    select new Firm.FirmPosition
                        {
                            FirmId = order.FirmId,
                            OrderId = orderItem.OrderId,
                            OrderPositionId = orderItem.OrderPositionId,
                            PackagePositionId = orderItem.PackagePositionId,
                            ItemPositionId = orderItem.ItemPositionId,

                            HasNoBinding = orderItem.CategoryId == null && orderItem.FirmAddressId == null,
                            Category1Id = category.L1Id,
                            Category3Id = category.L3Id,
                            FirmAddressId = orderItem.FirmAddressId,

                            Scope = order.AgileDistributionEndFactDate > firmPeriod.Begin ? Scope.Compute(order.WorkflowStep, order.Id) : order.Id,
                            Begin = firmPeriod.Begin,
                            End = firmPeriod.End,
                        };

                return principals.Distinct();
            }

            public FindSpecification<Firm.FirmPosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Firm.FirmPosition>(x => aggregateIds.Contains(x.FirmId));
            }
        }

        public sealed class FirmAssociatedPositionAccessor : DataChangesHandler<Firm.FirmAssociatedPosition>, IStorageBasedDataObjectAccessor<Firm.FirmAssociatedPosition>
        {
            private readonly IQuery _query;

            public FirmAssociatedPositionAccessor(IQuery query) : base(CreateInvalidator(x => GetRelatedOrders(query, x))) => _query = query;

            private static IRuleInvalidator CreateInvalidator(Func<IReadOnlyCollection<Firm.FirmAssociatedPosition>, IEnumerable<long>> func)
                => new RuleInvalidator
                    {
                        { MessageTypeCode.FirmAssociatedPositionMustHavePrincipal, func },
                        { MessageTypeCode.FirmAssociatedPositionMustHavePrincipalWithDifferentBindingObject, func },
                        { MessageTypeCode.FirmAssociatedPositionMustHavePrincipalWithMatchedBindingObject, func },
                    };

            private static IEnumerable<long> GetRelatedOrders(IQuery query, IReadOnlyCollection<Firm.FirmAssociatedPosition> dataObjects)
            {
                var firmIds = dataObjects.Select(x => x.FirmId).ToHashSet();
                return query.For<Firm.FirmPosition>().Where(x => firmIds.Contains(x.FirmId)).Select(x => x.OrderId).Distinct();
            }

            public IQueryable<Firm.FirmAssociatedPosition> GetSource()
            {
                const int BindingTypeMatch = 1;
                const int BindingTypeNoDependency = 2;

                var evaluatedRestrictions =
                    from order in _query.For<Facts::Order>()
                    from item in _query.For<Facts::OrderItem>().Where(x => x.OrderId == order.Id)
                    from project in _query.For<Facts::Project>().Where(x => x.OrganizationUnitId == order.DestOrganizationUnitId)
                    from rp in _query.For<Facts::Ruleset.RulesetProject>().Where(x => x.ProjectId == project.Id)
                    from rule in _query.For<Facts::Ruleset.AssociatedRule>().Where(x => x.AssociatedNomenclatureId == item.ItemPositionId)
                    where _query.For(Specs.Find.Facts.Ruleset)
                                .Any(x => x.Id == rule.RulesetId
                                            && x.Id == rp.RulesetId
                                            && x.BeginDate <= order.AgileDistributionStartDate
                                            && order.AgileDistributionStartDate < x.EndDate)
                    select new Firm.FirmAssociatedPosition
                        {
                            FirmId = order.FirmId,
                            OrderId = order.Id,

                            ItemPositionId = item.ItemPositionId,
                            OrderPositionId = item.OrderPositionId,
                            PackagePositionId = item.PackagePositionId,

                            PrincipalPositionId = rule.PrincipalNomenclatureId,
                            BindingType = rule.ConsideringBindingObject ? BindingTypeMatch : BindingTypeNoDependency
                        };

                return evaluatedRestrictions.Distinct();
            }

            public FindSpecification<Firm.FirmAssociatedPosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Firm.FirmAssociatedPosition>(x => aggregateIds.Contains(x.FirmId));
            }
        }

        public sealed class FirmDeniedPositionAccessor : DataChangesHandler<Firm.FirmDeniedPosition>, IStorageBasedDataObjectAccessor<Firm.FirmDeniedPosition>
        {
            private readonly IQuery _query;

            public FirmDeniedPositionAccessor(IQuery query) : base(CreateInvalidator(x => GetRelatedOrders(query, x))) => _query = query;

            private static IRuleInvalidator CreateInvalidator(Func<IReadOnlyCollection<Firm.FirmDeniedPosition>, IEnumerable<long>> func)
                => new RuleInvalidator
                    {
                        { MessageTypeCode.FirmPositionMustNotHaveDeniedPositions, func },
                    };

            private static IEnumerable<long> GetRelatedOrders(IQuery query, IReadOnlyCollection<Firm.FirmDeniedPosition> dataObjects)
            {
                var firmIds = dataObjects.Select(x => x.FirmId).ToHashSet();
                return query.For<Firm.FirmPosition>().Where(x => firmIds.Contains(x.FirmId)).Select(x => x.OrderId).Distinct();
            }

            public IQueryable<Firm.FirmDeniedPosition> GetSource()
            {
                var evaluatedRestrictions =
                    from order in _query.For<Facts::Order>()
                    from item in _query.For<Facts::OrderItem>().Where(x => x.OrderId == order.Id)
                    from project in _query.For<Facts::Project>().Where(x => x.OrganizationUnitId == order.DestOrganizationUnitId)
                    from rp in _query.For<Facts::Ruleset.RulesetProject>().Where(x => x.ProjectId == project.Id)
                    from ruleset in _query.For(Specs.Find.Facts.Ruleset)
                                          .Where(x => x.Id == rp.RulesetId
                                                      && x.BeginDate <= order.AgileDistributionStartDate
                                                      && order.AgileDistributionStartDate < x.EndDate)
                    from rule in _query.For<Facts::Ruleset.DeniedRule>().Where(x => x.RulesetId == ruleset.Id && x.NomenclatureId == item.ItemPositionId)
                    select new Firm.FirmDeniedPosition
                        {
                            FirmId = order.FirmId,
                            OrderId = order.Id,

                            OrderPositionId = item.OrderPositionId,
                            PackagePositionId = item.PackagePositionId,
                            ItemPositionId = item.ItemPositionId,

                            DeniedPositionId = rule.DeniedNomenclatureId,
                            BindingType = rule.BindingObjectStrategy,

                            Begin = ruleset.BeginDate,
                            End = ruleset.EndDate
                        };

                return evaluatedRestrictions.Distinct();
            }

            public FindSpecification<Firm.FirmDeniedPosition> GetFindSpecification(IReadOnlyCollection<ICommand> commands)
            {
                var aggregateIds = commands.Cast<ReplaceValueObjectCommand>().SelectMany(c => c.AggregateRootIds).ToHashSet();
                return new FindSpecification<Firm.FirmDeniedPosition>(x => aggregateIds.Contains(x.FirmId));
            }
        }
    }
}
