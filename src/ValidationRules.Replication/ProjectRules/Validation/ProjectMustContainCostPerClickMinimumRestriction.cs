using System.Collections.Generic;
using System.Linq;

using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.ProjectRules;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.ProjectRules.Validation
{
    /// <summary>
    /// Для проектов, где есть продажи в рубрики без указанного ограничения стоимости клика*, должна выводиться ошибка.
    /// "Для рубрики {0} в проекте {1} не указан минимальный CPC"
    /// 
    /// * учитываются ограничения только самой свежей версии для города.
    /// Source: IsCostPerClickRestrictionMissingOrderValidationRule
    /// </summary>
    public sealed class ProjectMustContainCostPerClickMinimumRestriction : ValidationResultAccessorBase
    {
        public ProjectMustContainCostPerClickMinimumRestriction(IQuery query) : base(query, MessageTypeCode.ProjectMustContainCostPerClickMinimumRestriction)
        {
        }

        protected override IQueryable<Version.ValidationResult> GetValidationResults(IQuery query)
        {
            // Даты, к наступлению которых требуется наличие действующих ограничений
            var requiredRestrictions =
                from order in query.For<Order>()
                from bid in query.For<Order.CostPerClickAdvertisement>().Where(x => x.OrderId == order.Id)
                let nextRelease = query.For<Project.NextRelease>().FirstOrDefault(x => x.ProjectId == order.ProjectId).Date
                let referenceDate = nextRelease > order.Start ? nextRelease : order.Start
                where referenceDate < order.End
                select new { OrderId = order.Id, order.ProjectId, Start = referenceDate, order.End, bid.CategoryId };

            var ruleResults =
                from req in requiredRestrictions
                let restrictionExist = query.For<Project.CostPerClickRestriction>().Any(x => x.ProjectId == req.ProjectId && x.Start <= req.Start && x.End > req.Start && x.CategoryId == req.CategoryId)
                where !restrictionExist
                select new Version.ValidationResult
                    {
                        MessageParams =
                            new MessageParams(
                                    new Dictionary<string, object> { { "start", req.Start } },
                                    new Reference<EntityTypeCategory>(req.CategoryId),
                                    new Reference<EntityTypeOrder>(req.OrderId),
                                    new Reference<EntityTypeProject>(req.ProjectId))
                                .ToXDocument(),

                        PeriodStart = req.Start,
                        PeriodEnd = req.End,
                        OrderId = req.OrderId,
                    };

            return ruleResults;
        }
    }
}