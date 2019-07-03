using System.Collections.Generic;
using System.Linq;

using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Storage.Identitites.EntityTypes;
using NuClear.ValidationRules.Storage.Model.Aggregates.ProjectRules;
using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Replication.ProjectRules.Validation
{
    /// <summary>
    /// Для позиций заказов, с продажами в рубрики, не соответствующие модели продаж позиции, должна выводиться ошибка
    /// "Позиция "{0}" не может быть продана в рубрику "{1}" проекта "{2}""
    /// 
    /// Source: SalesModelRestrictionsOrderValidationRule
    /// </summary>
    public sealed class OrderPositionSalesModelMustMatchCategorySalesModel : ValidationResultAccessorBase
    {
        public OrderPositionSalesModelMustMatchCategorySalesModel(IQuery query) : base(query, MessageTypeCode.OrderPositionSalesModelMustMatchCategorySalesModel)
        {
        }

        protected override IQueryable<Version.ValidationResult> GetValidationResults(IQuery query)
        {
            var ruleResults =
                from order in query.For<Order>()
                from restriction in query.For<Project.SalesModelRestriction>().Where(x => x.End > order.Start && order.End > x.Start && x.ProjectId == order.ProjectId)
                from adv in query.For<Order.CategoryAdvertisement>().Where(x => x.IsSalesModelRestrictionApplicable).Where(x => x.OrderId == order.Id && x.CategoryId == restriction.CategoryId)
                where restriction.SalesModel != adv.SalesModel
                let begin = order.Start > restriction.Start ? order.Start : restriction.Start
                select new Version.ValidationResult
                    {
                        MessageParams =
                            new MessageParams(
                                    new Dictionary<string, object> { { "begin", begin } },
                                    new Reference<EntityTypeCategory>(adv.CategoryId),
                                    new Reference<EntityTypeOrderPositionAdvertisement>(0,
                                        new Reference<EntityTypeOrderPosition>(adv.OrderPositionId),
                                        new Reference<EntityTypePosition>(adv.PositionId)),
                                    new Reference<EntityTypeOrder>(order.Id),
                                    new Reference<EntityTypeProject>(order.ProjectId))
                                .ToXDocument(),

                        PeriodStart = begin,
                        PeriodEnd = order.End < restriction.End ? order.End : restriction.End,
                        OrderId = order.Id,
                    };

            return ruleResults;
        }
    }
}