﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Replication.PriceRules.Validation.Dto;
using NuClear.ValidationRules.Replication.Specifications;
using NuClear.ValidationRules.Storage.Model.PriceRules.Aggregates;

using Version = NuClear.ValidationRules.Storage.Model.Messages.Version;

namespace NuClear.ValidationRules.Replication.PriceRules.Validation
{
    /// <summary>
    /// Для заказов в статусе "на расторжении", если они содержат основную позицию для остающихся одобренными заказов и среди одобренных нет другой основной позиции должно выводиться предупреждение.
    /// Позиция {0} данного Заказа является основной для следующих позиций:
    /// Позиция {0} Заказа {1}
    /// 
    /// Source: ADP/ADPValidation_Template
    /// 
    /// Q1: X является сопутствующей для Y (объект привязки совпадает)
    ///     Оформлен заказ №1 с X(A,B), Y(A) и заказ №2 с Y(B).
    ///     Заказ №2 отправляется на расторжение. Должна ли быть ошибка?
    /// A: Да.
    /// 
    /// Q2: Аналогично Q1, но позиция X ещё является сопутствующей для Z (без учёта)
    ///     И позиция Z продана в заказ 1. По логике (бага?) проверки LinkedObjectsMissedInPrincipalsActor (Q3) ошибка должна быть.
    /// A: Ошибка есть.
    /// </summary>
    public sealed class SatisfiedPrincipalPositionDifferentOrderActor : IActor
    {
        public const int MessageTypeId = 15;

        private static readonly int RuleResult = new ResultBuilder().WhenSingle(Result.Warning)
                                                                    .WhenMass(Result.Warning)
                                                                    .WhenMassPrerelease(Result.Warning)
                                                                    .WhenMassRelease(Result.Warning);

        private readonly ValidationRuleShared _validationRuleShared;

        public SatisfiedPrincipalPositionDifferentOrderActor(ValidationRuleShared validationRuleShared)
        {
            _validationRuleShared = validationRuleShared;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            return _validationRuleShared.ProcessRule(GetValidationResults, MessageTypeId);
        }

        private static IQueryable<Version.ValidationResult> GetValidationResults(IQuery query, long version)
        {
            var orderPositions =
                from order in query.For<Order>()
                join period in query.For<OrderPeriod>() on order.Id equals period.OrderId
                join position in query.For<OrderPosition>() on order.Id equals position.OrderId
                select new Dto<OrderPosition> { FirmId = order.FirmId, Start = period.Start, OrganizationUnitId = period.OrganizationUnitId, Scope = period.Scope, Position = position };

            var associatedPositions =
                from order in query.For<Order>()
                join period in query.For<OrderPeriod>() on order.Id equals period.OrderId
                join position in query.For<OrderAssociatedPosition>() on order.Id equals position.OrderId
                where period.Scope == 0
                select new Dto<OrderAssociatedPosition> { FirmId = order.FirmId, Start = period.Start, OrganizationUnitId = period.OrganizationUnitId, Scope = period.Scope, Position = position };

            var notSatisfiedPositions =
                associatedPositions.SelectMany(Specs.Join.Aggs.AvailablePrincipalPosition(orderPositions.Where(x => x.Scope == 0).DefaultIfEmpty()), (associated, principal) => new { associated, has = principal != null })
                                   .GroupBy(x => new
                                       {
                                           x.associated.Start,
                                           x.associated.Position.OrderId,
                                           x.associated.Position.CauseOrderPositionId,
                                           x.associated.Position.CauseItemPositionId,
                                           x.associated.Position.Category1Id,
                                           x.associated.Position.Category3Id,
                                           x.associated.Position.FirmAddressId,
                                       })
                                   .Where(x => x.Max(y => y.has) == false)
                                   .Select(x => x.Key);

            var satisfiedOnlyByHiddenPositions =
                associatedPositions.SelectMany(Specs.Join.Aggs.AvailablePrincipalPosition(orderPositions.Where(x => x.Scope != 0)), (associated, principal) => new { associated, principal })
                                   .Where(x => notSatisfiedPositions.Any(y => y.Start == x.associated.Start &&
                                                                              y.CauseOrderPositionId == x.associated.Position.CauseOrderPositionId &&
                                                                              y.CauseItemPositionId == x.associated.Position.CauseItemPositionId));

            var messages = from warning in satisfiedOnlyByHiddenPositions
                           join period in query.For<Period>() on new { warning.principal.Start, warning.principal.OrganizationUnitId } equals new { period.Start, period.OrganizationUnitId }
                           select new Version.ValidationResult
                           {
                               VersionId = version,
                               MessageType = MessageTypeId,
                               MessageParams =
                                       new XDocument(new XElement("root",
                                                                  new XElement("firm",
                                                                               new XAttribute("id", warning.principal.FirmId)),
                                                                  new XElement("position",
                                                                               new XAttribute("orderId", warning.principal.Position.OrderId),
                                                                               new XAttribute("orderNumber", query.For<Order>().Single(x => x.Id == warning.principal.Position.OrderId).Number),
                                                                               new XAttribute("orderPositionId", warning.principal.Position.OrderPositionId),
                                                                               new XAttribute("orderPositionName", query.For<Position>().Single(x => x.Id == warning.principal.Position.ItemPositionId).Name)),
                                                                  new XElement("position",
                                                                               new XAttribute("orderId", warning.associated.Position.OrderId),
                                                                               new XAttribute("orderNumber", query.For<Order>().Single(x => x.Id == warning.associated.Position.OrderId).Number),
                                                                               new XAttribute("orderPositionId", warning.associated.Position.CauseOrderPositionId),
                                                                               new XAttribute("orderPositionName", query.For<Position>().Single(x => x.Id == warning.associated.Position.CausePackagePositionId).Name)),
                                                                  new XElement("order",
                                                                               new XAttribute("id", warning.principal.Position.OrderId),
                                                                               new XAttribute("number", query.For<Order>().Single(x => x.Id == warning.principal.Position.OrderId).Number)))),

                               PeriodStart = period.Start,
                               PeriodEnd = period.End,
                               ProjectId = period.ProjectId,

                               Result = RuleResult,
                           };

            return messages;
        }
    }
}
