﻿using System.Linq;
using System.Xml.Linq;

using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Storage.Model.ConsistencyRules.Aggregates;

using Version = NuClear.ValidationRules.Storage.Model.Messages.Version;

namespace NuClear.ValidationRules.Replication.ConsistencyRules.Validation
{
    /// <summary>
    /// Для заказов, к которым привязана рубрика, не принадлежащая фирме, если объект привязки является "рубрика множественная со звёздочкой", должно выводиться информационное сообщение.
    /// "В позиции {0} найдена рубрика {1}, не принадлежащая фирме заказа"
    /// 
    /// Source: LinkingObjectsOrderValidationRule
    /// </summary>
    public sealed class LinkedCategoryAsterixMayBelongToFirm : ValidationResultAccessorBase
    {
        private static readonly int RuleResult = new ResultBuilder().WhenSingle(Result.Info)
                                                                    .WhenMass(Result.None)
                                                                    .WhenMassPrerelease(Result.Info)
                                                                    .WhenMassRelease(Result.Info);

        public LinkedCategoryAsterixMayBelongToFirm(IQuery query) : base(query, MessageTypeCode.LinkedCategoryAsterixMayBelongToFirm)
        {
        }

        protected override IQueryable<Version.ValidationResult> GetValidationResults(IQuery query)
        {
            var ruleResults = from order in query.For<Order>()
                              from category in query.For<Order.InvalidCategory>().Where(x => x.OrderId == order.Id)
                              where category.State == InvalidCategoryState.NotBelongToFirm && category.MayNotBelongToFirm
                              select new Version.ValidationResult
                                  {
                                      MessageParams = new XDocument(
                                          new XElement("root",
                                              new XElement("category",
                                                  new XAttribute("id", category.CategoryId),
                                                  new XAttribute("name", category.CategoryName)),
                                              new XElement("order",
                                                  new XAttribute("id", order.Id),
                                                  new XAttribute("number", order.Number)),
                                              new XElement("orderPosition",
                                                  new XAttribute("id", category.OrderPositionId),
                                                  new XAttribute("name", category.OrderPositionName)))),

                                      PeriodStart = order.BeginDistribution,
                                      PeriodEnd = order.EndDistributionPlan,
                                      OrderId = order.Id,

                                      Result = RuleResult,
                                  };

            return ruleResults;
        }
    }
}