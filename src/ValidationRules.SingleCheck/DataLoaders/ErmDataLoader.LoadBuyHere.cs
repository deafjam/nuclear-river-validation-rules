using LinqToDB.Data;
using NuClear.ValidationRules.Storage.Model.Erm;
using System.Linq;

using NuClear.ValidationRules.SingleCheck.Store;

namespace NuClear.ValidationRules.SingleCheck.DataLoaders
{
    public static partial class ErmDataLoader
    {
        private static void LoadBuyHere(DataConnection query, Order order, IStore store)
        {
            var categoryCodes =
                Storage.Model.Facts.Position.CategoryCodesPremiumPartnerAdvertising.Concat(
                Storage.Model.Facts.Position.CategoryCodesFmcgCutout).Concat(
                new[] {Storage.Model.Facts.Position.CategoryCodePartnerAdvertisingAddress});

            var positions =
                query.GetTable<Position>()
                     .Where(x => categoryCodes.Contains(x.CategoryCode))
                     .Execute();
            store.AddRange(positions);

            var firmAddresses = (
                from op in query.GetTable<OrderPosition>().Where(x => x.IsActive && !x.IsDeleted).Where(x => x.OrderId == order.Id)
                from opa in query.GetTable<OrderPositionAdvertisement>().Where(x => x.OrderPositionId == op.Id)
                from position in query.GetTable<Position>().Where(x => x.CategoryCode == Storage.Model.Facts.Position.CategoryCodePartnerAdvertisingAddress).Where(x => x.Id == opa.PositionId)
                from address in query.GetTable<FirmAddress>().Where(x => x.Id == opa.FirmAddressId)
                select address
            ).Execute();
            store.AddRange(firmAddresses);
            var firmAddressIds = firmAddresses.Select(x => x.Id).ToList();

            if (!firmAddresses.Any())
            {
                return;
            }

            var firmIds = firmAddresses.Select(x => x.FirmId).ToHashSet();

            // Нужны заказы этих фирм - вдруг фирма является рекламодателем.
            var firmOrders =
                from o in query.GetTable<Order>()
                    .Where(x => firmIds.Contains(x.FirmId))
                    .Where(x => new[] { 2, 4, 5 }.Contains(x.WorkflowStepId))
                    .Where(x => x.AgileDistributionStartDate < order.AgileDistributionEndPlanDate && order.AgileDistributionStartDate < x.AgileDistributionEndPlanDate)
                from op in query.GetTable<OrderPosition>()
                    .Where(x => x.IsActive && !x.IsDeleted)
                    .Where(x => x.OrderId == o.Id)
                from pp in query.GetTable<PricePosition>()
                    .Where(x => x.IsActive && !x.IsDeleted)
                    .Where(x => x.Id == op.PricePositionId)
                select new { Order = o, OrderPosition = op, PricePosition = pp };
            store.AddRange(firmOrders.Select(x => x.Order).Execute());
            store.AddRange(firmOrders.Select(x => x.OrderPosition).Execute());
            store.AddRange(firmOrders.Select(x => x.PricePosition).Execute());

            var positionIds = positions.Select(x => x.Id).ToList();

            // Нужны другие ЗМК заказы на те же самые адреса
            var xxxOrders =
                from o in query.GetTable<Order>()
                    .Where(x => new[] { 2, 4, 5 }.Contains(x.WorkflowStepId)) // заказы "на оформлении" не нужны - проверяемый их в любом лучае не видит
                    .Where(x => x.IsActive && !x.IsDeleted)
                    .Where(x => x.AgileDistributionStartDate < order.AgileDistributionEndPlanDate && order.AgileDistributionStartDate < x.AgileDistributionEndPlanDate && x.DestOrganizationUnitId == order.DestOrganizationUnitId)
                from op in query.GetTable<OrderPosition>()
                    .Where(x => x.IsActive && !x.IsDeleted)
                    .Where(x => x.OrderId == o.Id)
                from opa in query.GetTable<OrderPositionAdvertisement>()
                    .Where(x => positionIds.Contains(x.PositionId))
                    .Where(x => x.FirmAddressId == null || firmAddressIds.Contains(x.FirmAddressId.Value))
                    .Where(x => x.OrderPositionId == op.Id)
                select new { Order = o, OrderPosition = op, OrderPositionAdvertisement = opa };

            store.AddRange(xxxOrders.Select(x => x.Order).Distinct().Execute());
            store.AddRange(xxxOrders.Select(x => x.OrderPosition).Distinct().Execute());
            store.AddRange(xxxOrders.Select(x => x.OrderPositionAdvertisement).Distinct().Execute());
        }
    }
}
