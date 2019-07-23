using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using AccountAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.AccountRules;
using AdvertisementAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.AdvertisementRules;
using PriceAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;
using ProjectAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.ProjectRules;
using ConsistencyAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.ConsistencyRules;
using FirmAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules;
using ThemeAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.ThemeRules;
using SystemAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.SystemRules;

namespace NuClear.ValidationRules.Storage
{
    public static partial class Schema
    {
        private const string PriceAggregatesSchema = "PriceAggregates";
        private const string ProjectAggregatesSchema = "ProjectAggregates";
        private const string AccountAggregatesSchema = "AccountAggregates";
        private const string AdvertisementAggregatesSchema = "AdvertisementAggregates";
        private const string ConsistencyAggregatesSchema = "ConsistencyAggregates";
        private const string FirmAggregatesSchema = "FirmAggregates";
        private const string ThemeAggregatesSchema = "ThemeAggregates";
        private const string SystemAggregatesSchema = "SystemAggregates";

        public static MappingSchema Aggregates { get; } =
             new MappingSchema(nameof(Aggregates), new SqlServerMappingSchema())
                .RegisterDataTypes()
                .GetFluentMappingBuilder()
                .RegisterAccountAggregates()
                .RegisterAdvertisementAggregates()
                .RegisterConsistencyAggregates()
                .RegisterFirmAggregates()
                .RegisterPriceAggregates()
                .RegisterProjectAggregates()
                .RegisterSystemAggregates()
                .RegisterThemeAggregates()
                .MappingSchema;

        private static FluentMappingBuilder RegisterSystemAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<SystemAggregates::SystemStatus>()
                   .HasSchemaName(SystemAggregatesSchema)
                   .HasPrimaryKey(x => x.Id);

            return builder;
        }

        private static FluentMappingBuilder RegisterThemeAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<ThemeAggregates::Theme>()
                   .HasSchemaName(ThemeAggregatesSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<ThemeAggregates::Theme.InvalidCategory>()
                .HasSchemaName(ThemeAggregatesSchema)
                .HasPrimaryKey(x => new { x.ThemeId, x.CategoryId });

            builder.Entity<ThemeAggregates::Order>()
                   .HasSchemaName(ThemeAggregatesSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<ThemeAggregates::Order.OrderTheme>()
                    .HasSchemaName(ThemeAggregatesSchema)
                    .HasPrimaryKey(x => new { x.OrderId, x.ThemeId });

            builder.Entity<ThemeAggregates::Project>()
                   .HasSchemaName(ThemeAggregatesSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<ThemeAggregates::Project.ProjectDefaultTheme>()
                .HasSchemaName(ThemeAggregatesSchema)
                .HasPrimaryKey(x => new { x.ProjectId, x.ThemeId });

            return builder;
        }

        private static FluentMappingBuilder RegisterFirmAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<FirmAggregates::Firm>()
                   .HasSchemaName(FirmAggregatesSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<FirmAggregates::Firm.CategoryPurchase>()
                .HasSchemaName(FirmAggregatesSchema)
                .HasPrimaryKey(x => new { x.FirmId, x.Start, x.End, x.Scope, x.CategoryId });

            builder.Entity<FirmAggregates::Order>()
                   .HasSchemaName(FirmAggregatesSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => new { x.Start, x.End }, x => new { x.Scope, x.FirmId });

            builder.Entity<FirmAggregates::Order.FirmOrganizationUnitMismatch>()
                   .HasSchemaName(FirmAggregatesSchema)
                   .HasPrimaryKey(x => x.OrderId);

            builder.Entity<FirmAggregates::Order.InvalidFirm>()
                  .HasSchemaName(FirmAggregatesSchema)
                  .HasPrimaryKey(x => x.OrderId);

            builder.Entity<FirmAggregates::Order.PartnerPosition>()
                .HasSchemaName(FirmAggregatesSchema)
                .HasPrimaryKey(x => new {x.OrderId, x.DestinationFirmAddressId})
                .HasIndex(x => x.DestinationFirmId)
                .HasIndex(x => x.DestinationFirmAddressId, x => x.DestinationFirmId);

            builder.Entity<FirmAggregates::Order.PremiumPartnerPosition>()
                    .HasSchemaName(FirmAggregatesSchema)
                    .HasPrimaryKey(x => x.OrderId);

            builder.Entity<FirmAggregates::Order.FmcgCutoutPosition>()
                   .HasSchemaName(FirmAggregatesSchema)
                   .HasPrimaryKey(x => x.OrderId);

            return builder;
        }

        private static FluentMappingBuilder RegisterPriceAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<PriceAggregates::Firm>()
                  .HasSchemaName(PriceAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);

            builder.Entity<PriceAggregates::Firm.FirmPosition>()
                   .HasSchemaName(PriceAggregatesSchema)
                   // PK не получается создать, т.к. Category1Id, Category3Id, FirmAddressId nullable
                   .HasIndex(x => new { x.OrderPositionId, x.ItemPositionId, x.Category1Id, x.Category3Id, x.FirmAddressId, x.Start, x.End }, unique: true, clustered: true, name: "PK_Analog")
                   .HasIndex(x => x.FirmId)
                   .HasIndex(x => x.OrderId);

            builder.Entity<PriceAggregates::Firm.FirmAssociatedPosition>()
                   .HasSchemaName(PriceAggregatesSchema)
                   .HasPrimaryKey(x => new { x.OrderPositionId, x.ItemPositionId, x.PrincipalPositionId })
                   .HasIndex(x => x.FirmId);

            builder.Entity<PriceAggregates::Firm.FirmDeniedPosition>()
                   .HasSchemaName(PriceAggregatesSchema)
                   .HasPrimaryKey(x => new { x.OrderPositionId, x.ItemPositionId, x.DeniedPositionId })
                   .HasIndex(x => x.FirmId);

            builder.Entity<PriceAggregates::Order>()
                  .HasSchemaName(PriceAggregatesSchema)
                  .HasPrimaryKey(x => x.Id)
                  .HasIndex(x => x.IsCommitted, x => new {x.BeginDistribution, x.EndDistributionPlan});

            builder.Entity<PriceAggregates::Order.OrderPeriod>()
                   .HasSchemaName(PriceAggregatesSchema)
                   .HasPrimaryKey(x => new {x.OrderId, x.Start, x.End});

            builder.Entity<PriceAggregates::Order.OrderPricePosition>()
                   .HasSchemaName(PriceAggregatesSchema)
                   .HasPrimaryKey(x => new { x.OrderId, x.PriceId, x.OrderPositionId, x.PositionId });

            // таблица маленькая
            builder.Entity<PriceAggregates::Order.OrderCategoryPosition>()
                   .HasSchemaName(PriceAggregatesSchema);

            // таблица маленькая
            builder.Entity<PriceAggregates::Order.OrderThemePosition>()
                   .HasSchemaName(PriceAggregatesSchema);

            builder.Entity<PriceAggregates::Order.AmountControlledPosition>()
                  .HasSchemaName(PriceAggregatesSchema)
                  .HasPrimaryKey(x => new { x.OrderId, x.ProjectId, x.OrderPositionId});

            builder.Entity<PriceAggregates::Order.ActualPrice>()
                   .HasSchemaName(PriceAggregatesSchema)
                   .HasPrimaryKey(x => x.OrderId)
                   .HasIndex(x => x.PriceId);

            builder.Entity<PriceAggregates::Order.EntranceControlledPosition>()
                   .HasSchemaName(PriceAggregatesSchema)
                   .HasPrimaryKey(x => new { x.OrderId, x.FirmAddressId})
                   .HasIndex(x => x.EntranceCode);

            builder.Entity<PriceAggregates::Period>()
                  .HasSchemaName(PriceAggregatesSchema)
                  .HasPrimaryKey(x => x.Start);

            builder.Entity<PriceAggregates::Ruleset>()
                   .HasSchemaName(PriceAggregatesSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<PriceAggregates::Ruleset.AdvertisementAmountRestriction>()
                   .HasSchemaName(PriceAggregatesSchema)
                   .HasPrimaryKey(x => new { x.RulesetId, x.ProjectId, x.CategoryCode })
                   .HasIndex(x => new { x.ProjectId, x.CategoryCode}, x => new { x.Begin, x.End, x.Min, x.Max });

            return builder;
        }

        private static FluentMappingBuilder RegisterProjectAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<ProjectAggregates::Order>()
                   .HasSchemaName(ProjectAggregatesSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => x.IsDraft, x => new {x.ProjectId, x.Start, x.End})
                   .HasIndex(x => new { x.ProjectId, x.Start }, x => x.End);

            builder.Entity<ProjectAggregates::Order.AddressAdvertisementNonOnTheMap>()
                   .HasSchemaName(ProjectAggregatesSchema)
                   .HasPrimaryKey(x => new { x.OrderId, x.OrderPositionId, x.PositionId, x.AddressId });

            builder.Entity<ProjectAggregates::Order.CategoryAdvertisement>()
                   .HasSchemaName(ProjectAggregatesSchema)
                   .HasPrimaryKey(x => new { x.OrderId, x.OrderPositionId, x.CategoryId, x.PositionId })
                   .HasIndex(x => x.SalesModel);

            builder.Entity<ProjectAggregates::Order.CostPerClickAdvertisement>()
                   .HasSchemaName(ProjectAggregatesSchema)
                   .HasPrimaryKey(x => new { x.OrderId, x.OrderPositionId, x.CategoryId });

            builder.Entity<ProjectAggregates::Project>()
                   .HasSchemaName(ProjectAggregatesSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<ProjectAggregates::Project.Category>()
                   .HasSchemaName(ProjectAggregatesSchema)
                   .HasTableName("ProjectCategory")
                   .HasPrimaryKey(x => new { x.ProjectId, x.CategoryId });

            builder.Entity<ProjectAggregates::Project.CostPerClickRestriction>()
                   .HasSchemaName(ProjectAggregatesSchema)
                   .HasPrimaryKey(x => new { x.ProjectId, x.CategoryId, x.Start, x.End });

            builder.Entity<ProjectAggregates::Project.SalesModelRestriction>()
                   .HasSchemaName(ProjectAggregatesSchema)
                   .HasPrimaryKey(x => new { x.ProjectId, x.CategoryId, x.Start, x.End });

            builder.Entity<ProjectAggregates::Project.NextRelease>()
                   .HasSchemaName(ProjectAggregatesSchema)
                   .HasPrimaryKey(x => x.ProjectId);

            return builder;
        }

        private static FluentMappingBuilder RegisterAccountAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<AccountAggregates::Order>()
                  .HasSchemaName(AccountAggregatesSchema)
                  .HasPrimaryKey(x => x.Id)
                  .HasIndex(x => x.AccountId);

            builder.Entity<AccountAggregates::Order.DebtPermission>()
                  .HasSchemaName(AccountAggregatesSchema)
                  .HasPrimaryKey(x => new { x.OrderId, x.Start, x.End });

            builder.Entity<AccountAggregates::Account>()
                   .HasSchemaName(AccountAggregatesSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<AccountAggregates::Account.AccountPeriod>()
                   .HasSchemaName(AccountAggregatesSchema)
                   .HasPrimaryKey(x => new { x.AccountId, x.Start, x.End });

            return builder;
        }

        private static FluentMappingBuilder RegisterAdvertisementAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<AdvertisementAggregates::Order>()
                  .HasSchemaName(AdvertisementAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);

            builder.Entity<AdvertisementAggregates::Order.MissingAdvertisementReference>()
                .HasSchemaName(AdvertisementAggregatesSchema)
                .HasPrimaryKey(x => new { x.OrderId, x.OrderPositionId, x.CompositePositionId, x.PositionId })
                .HasIndex(x => x.AdvertisementIsOptional);

            builder.Entity<AdvertisementAggregates::Order.MissingOrderPositionAdvertisement>()
                   .HasSchemaName(AdvertisementAggregatesSchema)
                   .HasPrimaryKey(x => new { x.OrderId, x.OrderPositionId, x.PositionId });

            builder.Entity<AdvertisementAggregates::Order.AdvertisementFailedReview>()
                   .HasSchemaName(AdvertisementAggregatesSchema)
                   .HasPrimaryKey(x => new { x.OrderId, x.AdvertisementId });

            // таблица маленькая, можно обойтись без индексов
            builder.Entity<AdvertisementAggregates::Order.AdvertisementNotBelongToFirm>()
                   .HasSchemaName(AdvertisementAggregatesSchema);

            return builder;
        }

        private static FluentMappingBuilder RegisterConsistencyAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<ConsistencyAggregates::Order>()
                   .HasSchemaName(ConsistencyAggregatesSchema)
                   .HasPrimaryKey(x => x.Id)
                   .HasIndex(x => x.Id, x => new { x.Start, x.End });

            builder.Entity<ConsistencyAggregates::Order.BargainSignedLaterThanOrder>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => x.OrderId);

            builder.Entity<ConsistencyAggregates::Order.InvalidFirmAddress>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => new { x.OrderId, x.OrderPositionId, x.PositionId, x.FirmAddressId });

            // таблица маленькая, можно обойтись без индексов
            builder.Entity<ConsistencyAggregates::Order.CategoryNotBelongsToAddress>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.InvalidCategory>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => new { x.OrderId, x.OrderPositionId, x.PositionId, x.CategoryId });

            builder.Entity<ConsistencyAggregates::Order.HasNoAnyLegalPersonProfile>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => x.OrderId);

            builder.Entity<ConsistencyAggregates::Order.HasNoAnyPosition>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => x.OrderId);

            builder.Entity<ConsistencyAggregates::Order.InactiveReference>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => x.OrderId);

            builder.Entity<ConsistencyAggregates::Order.InvalidBillsTotal>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => x.OrderId);

            builder.Entity<ConsistencyAggregates::Order.LegalPersonProfileBargainExpired>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => new { x.OrderId, x.LegalPersonProfileId });

            builder.Entity<ConsistencyAggregates::Order.LegalPersonProfileWarrantyExpired>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => new { x.OrderId, x.LegalPersonProfileId });

            builder.Entity<ConsistencyAggregates::Order.MissingBargainScan>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => x.OrderId);

            builder.Entity<ConsistencyAggregates::Order.MissingBills>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => x.OrderId);

            builder.Entity<ConsistencyAggregates::Order.MissingRequiredField>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => x.OrderId);

            builder.Entity<ConsistencyAggregates::Order.MissingOrderScan>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => x.OrderId);

            // таблица маленькая
            builder.Entity<ConsistencyAggregates::Order.MissingValidPartnerFirmAddresses>()
                   .HasSchemaName(ConsistencyAggregatesSchema)
                   .HasIndex(x => x.OrderId);

            return builder;
        }
    }
}
