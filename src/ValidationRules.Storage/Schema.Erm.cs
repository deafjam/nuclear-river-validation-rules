using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using NuClear.ValidationRules.Storage.Model.Erm;

namespace NuClear.ValidationRules.Storage
{
    public static partial class Schema
    {
        private const string BillingSchema = "Billing";
        private const string BusinessDirectorySchema = "BusinessDirectory";
        private const string OrderValidationSchema = "OrderValidation";

        public static MappingSchema Erm { get; } =
            new MappingSchema(nameof(Aggregates), new SqlServerMappingSchema())
                .GetFluentMappingBuilder()
                .RegisterErm()
                .MappingSchema;

        private static FluentMappingBuilder RegisterErm(this FluentMappingBuilder builder)
        {
            builder.Entity<Account>().HasSchemaName(BillingSchema).HasTableName("Accounts").HasPrimaryKey(x => x.Id);
            builder.Entity<AccountDetail>().HasSchemaName(BillingSchema).HasTableName("AccountDetails").HasPrimaryKey(x => x.Id);
            builder.Entity<BranchOffice>().HasSchemaName(BillingSchema).HasTableName("BranchOffices").HasPrimaryKey(x => x.Id);
            builder.Entity<BranchOfficeOrganizationUnit>().HasSchemaName(BillingSchema).HasTableName("BranchOfficeOrganizationUnits").HasPrimaryKey(x => x.Id);
            builder.Entity<Deal>().HasSchemaName(BillingSchema).HasTableName("Deals").HasPrimaryKey(x => x.Id);
            builder.Entity<ReleaseInfo>().HasSchemaName(BillingSchema).HasTableName("ReleaseInfos").HasPrimaryKey(x => x.Id);
            builder.Entity<ReleaseWithdrawal>().HasSchemaName(BillingSchema).HasTableName("ReleasesWithdrawals").HasPrimaryKey(x => x.Id);
            builder.Entity<Order>().HasSchemaName(BillingSchema).HasTableName("Orders").HasPrimaryKey(x => x.Id);
            builder.Entity<OrderPosition>().HasSchemaName(BillingSchema).HasTableName("OrderPositions").HasPrimaryKey(x => x.Id);
            builder.Entity<OrderPositionCostPerClick>().HasSchemaName(BillingSchema).HasTableName("OrderPositionCostPerClicks");
            builder.Entity<OrderPositionAdvertisement>().HasSchemaName(BillingSchema).HasTableName("OrderPositionAdvertisement").HasPrimaryKey(x => x.Id);
            builder.Entity<Price>().HasSchemaName(BillingSchema).HasTableName("Prices").HasPrimaryKey(x => x.Id);
            builder.Entity<PricePosition>().HasSchemaName(BillingSchema).HasTableName("PricePositions").HasPrimaryKey(x => x.Id);
            builder.Entity<Project>().HasSchemaName(BillingSchema).HasTableName("Projects").HasPrimaryKey(x => x.Id);
            builder.Entity<Position>().HasSchemaName(BillingSchema).HasTableName("Positions").HasPrimaryKey(x => x.Id);
            builder.Entity<PositionChild>().HasSchemaName(BillingSchema).HasTableName("PositionChildren");
            builder.Entity<Category>().HasSchemaName(BusinessDirectorySchema).HasTableName("Categories").HasPrimaryKey(x => x.Id);
            builder.Entity<CategoryOrganizationUnit>().HasSchemaName(BusinessDirectorySchema).HasTableName("CategoryOrganizationUnits").HasPrimaryKey(x => x.Id);
            builder.Entity<CategoryFirmAddress>().HasSchemaName(BusinessDirectorySchema).HasTableName("CategoryFirmAddresses").HasPrimaryKey(x => x.Id);
            builder.Entity<CostPerClickCategoryRestriction>().HasSchemaName(BusinessDirectorySchema).HasTableName("CostPerClickCategoryRestrictions");
            builder.Entity<SalesModelCategoryRestriction>().HasSchemaName(BusinessDirectorySchema).HasTableName("SalesModelCategoryRestrictions");
            builder.Entity<Theme>().HasSchemaName(BillingSchema).HasTableName("Themes").HasPrimaryKey(x => x.Id);
            builder.Entity<ThemeCategory>().HasSchemaName(BillingSchema).HasTableName("ThemeCategories").HasPrimaryKey(x => x.Id);
            builder.Entity<ThemeOrganizationUnit>().HasSchemaName(BillingSchema).HasTableName("ThemeOrganizationUnits").HasPrimaryKey(x => x.Id);
            
            builder.Entity<Bargain>().HasSchemaName(BillingSchema).HasTableName("Bargains").HasPrimaryKey(x => x.Id);
            builder.Entity<BargainFile>().HasSchemaName(BillingSchema).HasTableName("BargainFiles").HasPrimaryKey(x => x.Id);
            builder.Entity<Bill>().HasSchemaName(BillingSchema).HasTableName("Bills").HasPrimaryKey(x => x.Id);
            builder.Entity<Firm>().HasSchemaName(BusinessDirectorySchema).HasTableName("Firms").HasPrimaryKey(x => x.Id);
            builder.Entity<FirmAddress>().HasSchemaName(BusinessDirectorySchema).HasTableName("FirmAddresses").HasPrimaryKey(x => x.Id);
            builder.Entity<LegalPerson>().HasSchemaName(BillingSchema).HasTableName("LegalPersons").HasPrimaryKey(x => x.Id);
            builder.Entity<LegalPersonProfile>().HasSchemaName(BillingSchema).HasTableName("LegalPersonProfiles").HasPrimaryKey(x => x.Id);
            builder.Entity<OrderFile>().HasSchemaName(BillingSchema).HasTableName("OrderFiles").HasPrimaryKey(x => x.Id);
            
            builder.Entity<UnlimitedOrder>().HasSchemaName(OrderValidationSchema).HasTableName("UnlimitedOrders");
            
            builder.Entity<UseCaseTrackingEvent>().HasSchemaName("Shared").HasTableName("UseCaseTrackingEvents");
            
            builder.Entity<NomenclatureCategory>().HasSchemaName(BillingSchema).HasTableName("NomenclatureCategories");

            return builder;
        }
    }
}
