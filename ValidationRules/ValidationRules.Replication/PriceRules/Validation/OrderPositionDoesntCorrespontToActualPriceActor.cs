using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.Storage.API.Readings;
using NuClear.ValidationRules.Storage.Model.PriceRules.Aggregates;

using Version = NuClear.ValidationRules.Storage.Model.Messages.Version;

namespace NuClear.ValidationRules.Replication.PriceRules.Validation
{
    public sealed class OrderPositionDoesntCorrespontToActualPriceActor : IActor
    {
        // OrderCheckOrderPositionDoesntCorrespontToActualPrice - ������� {OrderPositionId} �� ������������� ����������� �����-�����. ���������� ������� ������� �� �������� ������������ �����-�����.
        private const int MessageTypeId = 5;

        private static readonly int RuleResult = new ResultBuilder().WhenSingle(Result.Error)
                                                                    .WhenMass(Result.Warning)
                                                                    .WhenMassPrerelease(Result.Warning)
                                                                    .WhenMassRelease(Result.Warning); // ������� ��������� ���������, �������� erm ����� ������ ��� �������, �������� �� Approved

        private readonly ValidationRuleShared _validationRuleShared;

        public OrderPositionDoesntCorrespontToActualPriceActor(ValidationRuleShared validationRuleShared)
        {
            _validationRuleShared = validationRuleShared;
        }

        public IReadOnlyCollection<IEvent> ExecuteCommands(IReadOnlyCollection<ICommand> commands)
        {
            return _validationRuleShared.ProcessRule(GetValidationResults, MessageTypeId);
        }

        private static IQueryable<Version.ValidationResult> GetValidationResults(IQuery query, long version)
        {
            // �������� ��������� ������������ ������ ������� �������
            var orderFirstPeriods = from orderPeriod1 in query.For<OrderPeriod>()
                                    from orderPeriod2 in query.For<OrderPeriod>().Where(x => orderPeriod1.OrderId == x.OrderId && orderPeriod1.Start > x.Start).DefaultIfEmpty()
                                    where orderPeriod2 == null
                                    select orderPeriod1;

            var orderFirstPeriodDtos = from orderFirstPeriod in orderFirstPeriods
                                  join order in query.For<Order>() on orderFirstPeriod.OrderId equals order.Id
                                  join period in query.For<Period>()
                                  on new { orderFirstPeriod.OrganizationUnitId, orderFirstPeriod.Start } equals new { period.OrganizationUnitId, period.Start }
                                  select new
                                  {
                                      OrderId = order.Id,

                                      period.OrganizationUnitId,
                                      period.ProjectId,
                                      period.Start,
                                      period.End,
                                  };

            var orderPositionBadPriceErrors =
            from orderFirstPeriodDto in orderFirstPeriodDtos
            from pricePeriod in query.For<PricePeriod>().Where(x => x.OrganizationUnitId == orderFirstPeriodDto.OrganizationUnitId && x.Start == orderFirstPeriodDto.Start).DefaultIfEmpty()
            join orderPricePosition in query.For<OrderPricePosition>() on orderFirstPeriodDto.OrderId equals orderPricePosition.OrderId
            where pricePeriod != null
            where pricePeriod.PriceId != orderPricePosition.PriceId
            select new Version.ValidationResult
            {
                MessageType = MessageTypeId,
                MessageParams = new XDocument(new XElement("empty",
                                                new XAttribute("id", orderPricePosition.OrderPositionId),
                                                new XAttribute("name", orderPricePosition.OrderPositionName)
                                             )),
                PeriodStart = orderFirstPeriodDto.Start,
                PeriodEnd = orderFirstPeriodDto.End,
                ProjectId = orderFirstPeriodDto.ProjectId,
                VersionId = version,

                ReferenceType = EntityTypeIds.Order,
                ReferenceId = orderFirstPeriodDto.OrderId,

                Result = RuleResult,
            };

            return orderPositionBadPriceErrors;
        }
    }
}