﻿using System;
using System.Linq;

using NuClear.Storage.API.Readings;
using NuClear.Storage.API.Specifications;

namespace NuClear.ValidationRules.Replication.Specifications
{
    using Facts = Storage.Model.Facts;
    using Aggregates = Storage.Model.Aggregates;

    public static partial class Specs
    {
        public static partial class Map
        {
            public static class Facts
            {
                // ReSharper disable once InconsistentNaming
                public static class ToAggregates
                {
                    public static readonly MapSpecification<IQuery, IQueryable<Aggregates::Ruleset>> Rulesets
                        = new MapSpecification<IQuery, IQueryable<Aggregates::Ruleset>>(
                            q =>
                            {
                                return q.For<Facts::RulesetRule>().OrderByDescending(x => x.Priority).Take(1).Select(x => new Aggregates::Ruleset
                                {
                                    Id = x.Id
                                });
                            });

                    public static readonly MapSpecification<IQuery, IQueryable<Aggregates::RulesetRule>> RulesetRules
                        = new MapSpecification<IQuery, IQueryable<Aggregates::RulesetRule>>(
                            q =>
                                {
                                    var queгу = q.For<Facts::RulesetRule>();

                                    return queгу
                                            .Select(x => new
                                            {
                                                Rule = x,
                                                MaxPriority = queгу.Max(y => y.Priority)
                                            })
                                            .Where(x => x.Rule.Priority == x.MaxPriority)
                                            .Select(x => x.Rule)
                                            .Select(x => new Aggregates::RulesetRule
                                            {
                                                RulesetId = x.Id,
                                                RuleType = x.RuleType,

                                                DependentPositionId = x.DependentPositionId,
                                                PrincipalPositionId = x.PrincipalPositionId,
                                                ObjectBindingType = x.ObjectBindingType,
                                            });
                                });

                    public static readonly MapSpecification<IQuery, IQueryable<Aggregates::Order>> Orders
                        = new MapSpecification<IQuery, IQueryable<Aggregates::Order>>(
                            q => q.For<Facts::Order>().Select(x => new Aggregates::Order
                                {
                                    Id = x.Id,
                                    FirmId = x.FirmId,
                                }));

                    public static readonly MapSpecification<IQuery, IQueryable<Aggregates::Position>> Positions
                        = new MapSpecification<IQuery, IQueryable<Aggregates::Position>>(
                            q => q.For<Facts::Position>().Select(x => new Aggregates::Position
                                {
                                    Id = x.Id,
                                    CategoryCode = x.CategoryCode,
                                    IsControlledByAmount = x.IsControlledByAmount,
                                    Name = x.Name
                                }));

                    public static readonly MapSpecification<IQuery, IQueryable<Aggregates::Period>> Periods
                        = new MapSpecification<IQuery, IQueryable<Aggregates::Period>>(
                            q =>
                                {
                                    var dates = q.For<Facts::Order>()
                                                 .Select(x => new { Date = x.BeginDistributionDate, OrganizationUnitId = x.DestOrganizationUnitId })
                                                 .Union(q.For<Facts::Order>().Select(x => new { Date = x.EndDistributionDateFact, OrganizationUnitId = x.DestOrganizationUnitId }))
                                                 .Union(q.For<Facts::Order>().Select(x => new { Date = x.EndDistributionDatePlan, OrganizationUnitId = x.DestOrganizationUnitId }))
                                                 .Union(q.For<Facts::Price>().Select(x => new { Date = x.BeginDate, x.OrganizationUnitId }))
                                                 .SelectMany(x => q.For<Facts::Project>().Where(p => p.OrganizationUnitId == x.OrganizationUnitId).DefaultIfEmpty(), (x, p) => new { x.Date, x.OrganizationUnitId, ProjectId = p.Id })
                                                 .OrderBy(x => x.Date)
                                                 .Distinct();

                                    return dates.Select(x => new { Start = x, End = dates.FirstOrDefault(y => y.Date > x.Date && y.OrganizationUnitId == x.OrganizationUnitId) })
                                                      .Select(x => new Aggregates::Period
                                                          {
                                                              Start = x.Start.Date,
                                                              End = x.End != null ? x.End.Date : DateTime.MaxValue,
                                                              OrganizationUnitId = x.Start.OrganizationUnitId,
                                                              ProjectId = x.Start.ProjectId
                                                          });
                                });

                    public static readonly MapSpecification<IQuery, IQueryable<Aggregates::PricePeriod>> PricePeriods
                        = new MapSpecification<IQuery, IQueryable<Aggregates::PricePeriod>>(
                            q =>
                                {
                                    var dates = q.For<Facts::Order>()
                                                 .Select(x => new { Date = x.BeginDistributionDate, OrganizationUnitId = x.DestOrganizationUnitId })
                                                 .Union(q.For<Facts::Order>().Select(x => new { Date = x.EndDistributionDateFact, OrganizationUnitId = x.DestOrganizationUnitId }))
                                                 .Union(q.For<Facts::Order>().Select(x => new { Date = x.EndDistributionDatePlan, OrganizationUnitId = x.DestOrganizationUnitId }))
                                                 .Union(q.For<Facts::Price>().Select(x => new { Date = x.BeginDate, x.OrganizationUnitId }))
                                                 .Distinct();

                                    var result = dates.Select(date => new
                                                                    {
                                                                        PriceId = (long?)q.For<Facts::Price>()
                                                                                            .Where(price => price.OrganizationUnitId == date.OrganizationUnitId && price.BeginDate <= date.Date)
                                                                                            .OrderByDescending(price => price.BeginDate)
                                                                                            .FirstOrDefault()
                                                                                            .Id,
                                                                        Period = date
                                                                    })
                                                      .Where(x => x.PriceId.HasValue)
                                                      .Select(x => new Aggregates::PricePeriod
                                                                    {
                                                                        OrganizationUnitId = x.Period.OrganizationUnitId,
                                                                        PriceId = x.PriceId.Value,
                                                                        Start = x.Period.Date
                                                                    });

                                    return result;
                                });
                }
            }
        }
    }
}