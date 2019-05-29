using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Storage
{
    public static partial class Schema
    {
        private const string FactsSchema = "Facts";

        public static MappingSchema Facts { get; } =
            new MappingSchema(nameof(Facts), new SqlServerMappingSchema())
                .RegisterDataTypes()
                .GetFluentMappingBuilder()
                .RegisterFacts()
                .MappingSchema;

        private static FluentMappingBuilder RegisterFacts(this FluentMappingBuilder builder)
        {
            builder.Entity<Account>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<AccountDetail>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => new { x.OrderId, x.PeriodStartDate });

            builder.Entity<Advertisement>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<Bargain>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<BargainScanFile>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<Bill>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => x.OrderId, x => x.PayablePlan);

            builder.Entity<BranchOffice>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<BranchOfficeOrganizationUnit>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<Category>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<CategoryOrganizationUnit>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => x.OrganizationUnitId, x => x.CategoryId);

            builder.Entity<CostPerClickCategoryRestriction>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => new { x.ProjectId, x.CategoryId, x.Begin });

            builder.Entity<Deal>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<EntityName>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => new { x.Id, x.EntityType });

            builder.Entity<Firm>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => new {x.IsActive, x.IsDeleted, x.IsClosedForAscertainment}, x => x.OrganizationUnitId);

            builder.Entity<FirmAddress>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => new { x.FirmId, x.IsActive, x.IsDeleted, x.IsClosedForAscertainment }, x => x.Id)
                   .HasIndex(x => x.IsLocatedOnTheMap);

            builder.Entity<FirmAddressCategory>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => new {x.Id, x.CategoryId })
                   .HasIndex(x => x.CategoryId, x => x.FirmAddressId)
                   .HasIndex(x => new { x.FirmAddressId, x.CategoryId });

            builder.Entity<LegalPerson>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<LegalPersonProfile>()
                .HasSchemaName(FactsSchema)
                .HasPrimaryKey(x => x.Id)
                .HasIndex(x => x.LegalPersonId)
                .HasIndex(x => x.WarrantyEndDate, x => x.LegalPersonId)
                .HasIndex(x => x.BargainEndDate, x => x.LegalPersonId);

            builder.Entity<NomenclatureCategory>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<Order>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => x.DestOrganizationUnitId, x => new { x.Id, x.FirmId, x.BeginDistribution, x.EndDistributionFact, x.EndDistributionPlan, x.WorkflowStep, x.IsSelfAds })
                   .HasIndex(x => new { x.LegalPersonId, x.SignupDate }, x => x.Id)
                   .HasIndex(x => x.BargainId, x => x.Id)
                   .HasIndex(x => new { x.BargainId, x.SignupDate }, x => x.Id)
                   .HasIndex(x => new { x.BeginDistribution, x.DestOrganizationUnitId }, x => x.FirmId)
                   .HasIndex(x => x.EndDistributionFact)
                   .HasIndex(x => x.EndDistributionPlan)
                   .HasIndex(x => x.FirmId, x => new { x.DestOrganizationUnitId, x.BeginDistribution });

            builder.Entity<OrderItem>()
                   .HasSchemaName(FactsSchema)
                   // PK не получается создать, т.к. PricePositionId, FirmAddressId, CategoryId nullable
                   .HasIndex(x => new { x.OrderId, x.OrderPositionId, x.PackagePositionId, x.ItemPositionId, x.PricePositionId, x.FirmAddressId, x.CategoryId}, clustered: true, unique: true, name: "PK_Analog");

            builder.Entity<OrderPosition>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => x.OrderId, x => new { x.PricePositionId })
                   .HasIndex(x => x.PricePositionId, x => new { x.OrderId });

            builder.Entity<OrderPositionAdvertisement>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => x.AdvertisementId, x => new { x.OrderPositionId, x.PositionId })
                   .HasIndex(x => x.OrderPositionId, x => new { x.FirmAddressId, x.PositionId })
                   .HasIndex(x => new {x.PositionId, x.FirmAddressId}, x => x.OrderPositionId)
                   .HasIndex(x => new { x.FirmAddressId, x.CategoryId }, x => new { x.OrderPositionId, x.PositionId })
                   .HasIndex(x => x.CategoryId, x => new { x.OrderPositionId, x.PositionId, x.FirmAddressId })
                   .HasIndex(x => x.ThemeId, x => x.OrderPositionId);

            builder.Entity<OrderPositionCostPerClick>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => new { x.OrderPositionId, x.CategoryId });

            builder.Entity<OrderScanFile>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => x.OrderId);

            builder.Entity<Position>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<PositionChild>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => new {x.MasterPositionId, x.ChildPositionId });

            builder.Entity<Price>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => new { x.BeginDate, x.ProjectId });

            builder.Entity<PricePosition>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => x.PriceId)
                   .HasIndex(x => x.PositionId);

            builder.Entity<Project>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<ReleaseInfo>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => x.PeriodEndDate);

            builder.Entity<ReleaseWithdrawal>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => new { x.OrderPositionId, x.Start });

            builder.Entity<SalesModelCategoryRestriction>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => new { x.ProjectId, x.CategoryId, x.Begin });

            builder.Entity<SystemStatus>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<Theme>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<ThemeCategory>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<ThemeOrganizationUnit>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<UnlimitedOrder>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => new { x.OrderId, x.PeriodStart });

            builder.Entity<Ruleset>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => x.IsDeleted, x => new { x.BeginDate, x.EndDate });

            builder.Entity<Ruleset.AssociatedRule>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => new { x.RulesetId, x.AssociatedNomenclatureId, x.PrincipalNomenclatureId });

            builder.Entity<Ruleset.DeniedRule>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => new { x.RulesetId, x.NomenclatureId, x.DeniedNomenclatureId });

            builder.Entity<Ruleset.QuantitativeRule>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => new { x.RulesetId, x.NomenclatureCategoryCode });

            builder.Entity<Ruleset.RulesetProject>()
                   .HasSchemaName(FactsSchema)
                   .HasPrimaryKey(x => new { x.ProjectId, x.RulesetId });

            return builder;
        }
    }
}
