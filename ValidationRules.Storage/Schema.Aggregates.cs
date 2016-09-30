﻿using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using AccountAggregates = NuClear.ValidationRules.Storage.Model.AccountRules.Aggregates;
using AdvertisementAggregates = NuClear.ValidationRules.Storage.Model.AdvertisementRules.Aggregates;
using PriceAggregates = NuClear.ValidationRules.Storage.Model.PriceRules.Aggregates;
using ConsistencyAggregates = NuClear.ValidationRules.Storage.Model.ConsistencyRules.Aggregates;
using FirmAggregates = NuClear.ValidationRules.Storage.Model.FirmRules.Aggregates;

namespace NuClear.ValidationRules.Storage
{
    public static partial class Schema
    {
        private const string PriceAggregatesSchema = "PriceAggregates";
        private const string AccountAggregatesSchema = "AccountAggregates";
        private const string AdvertisementAggregatesSchema = "AdvertisementAggregates";
        private const string ConsistencyAggregatesSchema = "ConsistencyAggregates";
        private const string FirmAggregatesSchema = "FirmAggregates";

        public static MappingSchema Aggregates
            => new MappingSchema(nameof(Aggregates), new SqlServerMappingSchema())
                .GetFluentMappingBuilder()
                .RegisterPriceAggregates()
                .RegisterAccountAggregates()
                .RegisterAdvertisementAggregates()
                .RegisterConsistencyAggregates()
                .RegisterFirmAggregates()
                .MappingSchema;

        private static FluentMappingBuilder RegisterFirmAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<FirmAggregates::Firm>()
                   .HasSchemaName(FirmAggregatesSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<FirmAggregates::Order>()
                   .HasSchemaName(FirmAggregatesSchema)
                   .HasPrimaryKey(x => x.Id);

            builder.Entity<FirmAggregates::Order.CategoryPurchase>()
                   .HasSchemaName(FirmAggregatesSchema);

            builder.Entity<FirmAggregates::Order.SpecialPosition>()
                   .HasSchemaName(FirmAggregatesSchema);

            builder.Entity<FirmAggregates::Order.FirmOrganiationUnitMismatch>()
                   .HasSchemaName(FirmAggregatesSchema);

            return builder;
        }

        private static FluentMappingBuilder RegisterPriceAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<PriceAggregates::Price>()
                  .HasSchemaName(PriceAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);

            builder.Entity<PriceAggregates::AssociatedPositionGroupOvercount>()
                  .HasSchemaName(PriceAggregatesSchema);

            builder.Entity<PriceAggregates::AdvertisementAmountRestriction>()
                  .HasSchemaName(PriceAggregatesSchema);

            builder.Entity<PriceAggregates::Order>()
                  .HasSchemaName(PriceAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);

            builder.Entity<PriceAggregates::OrderPeriod>()
                  .HasSchemaName(PriceAggregatesSchema);

            builder.Entity<PriceAggregates::OrderPosition>()
                  .HasSchemaName(PriceAggregatesSchema);

            builder.Entity<PriceAggregates::OrderAssociatedPosition>()
                  .HasSchemaName(PriceAggregatesSchema);

            builder.Entity<PriceAggregates::OrderDeniedPosition>()
                  .HasSchemaName(PriceAggregatesSchema);

            builder.Entity<PriceAggregates::OrderPricePosition>()
                  .HasSchemaName(PriceAggregatesSchema);

            builder.Entity<PriceAggregates::AmountControlledPosition>()
                  .HasSchemaName(PriceAggregatesSchema);

            builder.Entity<PriceAggregates::Period>()
                  .HasSchemaName(PriceAggregatesSchema)
                  .HasPrimaryKey(x => x.Start)
                  .HasPrimaryKey(x => x.End)
                  .HasPrimaryKey(x => x.ProjectId);

            builder.Entity<PriceAggregates::PricePeriod>()
                  .HasSchemaName(PriceAggregatesSchema);

            builder.Entity<PriceAggregates::Position>()
                  .HasSchemaName(PriceAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);

            builder.Entity<PriceAggregates::Project>()
                  .HasSchemaName(PriceAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);

            builder.Entity<PriceAggregates::Theme>()
                  .HasSchemaName(PriceAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);

            builder.Entity<PriceAggregates::Category>()
                  .HasSchemaName(PriceAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);

            return builder;
        }

        private static FluentMappingBuilder RegisterAccountAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<AccountAggregates::Order>()
                  .HasSchemaName(AccountAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);

            builder.Entity<AccountAggregates::Lock>()
                   .HasSchemaName(AccountAggregatesSchema);

            builder.Entity<AccountAggregates::Account>()
                   .HasSchemaName(AccountAggregatesSchema);

            builder.Entity<AccountAggregates::AccountPeriod>()
                   .HasSchemaName(AccountAggregatesSchema);

            return builder;
        }

        private static FluentMappingBuilder RegisterAdvertisementAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<AdvertisementAggregates::Order>()
                  .HasSchemaName(AdvertisementAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);
            builder.Entity<AdvertisementAggregates::Order.LinkedProject>()
                   .HasSchemaName(AdvertisementAggregatesSchema);
            builder.Entity<AdvertisementAggregates::Order.MissingAdvertisementReference>()
                   .HasSchemaName(AdvertisementAggregatesSchema);
            builder.Entity<AdvertisementAggregates::Order.MissingOrderPositionAdvertisement>()
                   .HasSchemaName(AdvertisementAggregatesSchema);
            builder.Entity<AdvertisementAggregates::Order.AdvertisementDeleted>()
                   .HasSchemaName(AdvertisementAggregatesSchema);
            builder.Entity<AdvertisementAggregates::Order.AdvertisementMustBelongToFirm>()
                   .HasSchemaName(AdvertisementAggregatesSchema);
            builder.Entity<AdvertisementAggregates::Order.AdvertisementIsDummy>()
                   .HasSchemaName(AdvertisementAggregatesSchema);
            builder.Entity<AdvertisementAggregates::Order.OrderAdvertisement>()
                   .HasSchemaName(AdvertisementAggregatesSchema);

            builder.Entity<AdvertisementAggregates::Advertisement>()
                  .HasSchemaName(AdvertisementAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);
            builder.Entity<AdvertisementAggregates::Advertisement.RequiredElementMissing>()
                  .HasSchemaName(AdvertisementAggregatesSchema);
            builder.Entity<AdvertisementAggregates::Advertisement.ElementNotPassedReview>()
                  .HasSchemaName(AdvertisementAggregatesSchema);

            builder.Entity<AdvertisementAggregates::AdvertisementElementTemplate>()
                  .HasSchemaName(AdvertisementAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);

            builder.Entity<AdvertisementAggregates::Firm>()
                  .HasSchemaName(AdvertisementAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);
            builder.Entity<AdvertisementAggregates::Firm.WhiteListDistributionPeriod>()
                  .HasSchemaName(AdvertisementAggregatesSchema);

            builder.Entity<AdvertisementAggregates::Position>()
                  .HasSchemaName(AdvertisementAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);

            return builder;
        }

        private static FluentMappingBuilder RegisterConsistencyAggregates(this FluentMappingBuilder builder)
        {
            builder.Entity<ConsistencyAggregates::Order>()
                  .HasSchemaName(ConsistencyAggregatesSchema)
                  .HasPrimaryKey(x => x.Id);

            builder.Entity<ConsistencyAggregates::Order.BargainSignedLaterThanOrder>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.InvalidFirm>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.InvalidFirmAddress>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.InvalidCategoryFirmAddress>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.InvalidCategory>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.HasNoAnyLegalPersonProfile>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.HasNoAnyPosition>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.InvalidBeginDistributionDate>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.InvalidBillsPeriod>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.InvalidBillsTotal>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.InvalidEndDistributionDate>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.LegalPersonProfileBargainExpired>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.LegalPersonProfileWarrantyExpired>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.MissingBargainScan>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.MissingBills>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.MissingRequiredField>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            builder.Entity<ConsistencyAggregates::Order.MissingOrderScan>()
                  .HasSchemaName(ConsistencyAggregatesSchema);

            return builder;
        }
    }
}
