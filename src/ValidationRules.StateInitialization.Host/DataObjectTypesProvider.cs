using System;
using System.Collections.Generic;
using System.Linq;
using Facts = NuClear.ValidationRules.Storage.Model.Facts;
using AccountAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.AccountRules;
using AdvertisementAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.AdvertisementRules;
using ThemeAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.ThemeRules;
using ConsistencyAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.ConsistencyRules;
using FirmAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules;
using PriceAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.PriceRules;
using ProjectAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.ProjectRules;
using SystemAggregates = NuClear.ValidationRules.Storage.Model.Aggregates.SystemRules;
using Messages = NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.StateInitialization.Host
{
    public static class DataObjectTypesProvider
    {
        public static readonly Type[] ErmFactTypes =
            {
                typeof(Facts::Account),
                typeof(Facts::AccountDetail),
                typeof(Facts::Bargain),
                typeof(Facts::BargainScanFile),
                typeof(Facts::Bill),
                typeof(Facts::BranchOffice),
                typeof(Facts::BranchOfficeOrganizationUnit),
                typeof(Facts::Category),
                typeof(Facts::CategoryOrganizationUnit),
                typeof(Facts::CostPerClickCategoryRestriction),
                typeof(Facts::Deal),
                typeof(Facts::EntityName),
                typeof(Facts::Firm),
                typeof(Facts::FirmInactive),
                typeof(Facts::FirmAddress),
                typeof(Facts::FirmAddressInactive),
                typeof(Facts::FirmAddressCategory),
                typeof(Facts::LegalPerson),
                typeof(Facts::LegalPersonProfile),
                typeof(Facts::NomenclatureCategory),
                typeof(Facts::Order),
                typeof(Facts::OrderConsistency),
                typeof(Facts::OrderItem),
                typeof(Facts::OrderPosition),
                typeof(Facts::OrderPositionAdvertisement),
                typeof(Facts::OrderPositionCostPerClick),
                typeof(Facts::OrderScanFile),
                typeof(Facts::OrderWorkflow),
                typeof(Facts::Position),
                typeof(Facts::PositionChild),
                typeof(Facts::Price),
                typeof(Facts::PricePosition),
                typeof(Facts::Project),
                typeof(Facts::ReleaseInfo),
                typeof(Facts::ReleaseWithdrawal),
                typeof(Facts::SalesModelCategoryRestriction),
                typeof(Facts::SystemStatus),
                typeof(Facts::Theme),
                typeof(Facts::ThemeCategory),
                typeof(Facts::ThemeOrganizationUnit),
                typeof(Facts::UnlimitedOrder),
            };

        internal static readonly Type[] AmsFactTypes =
            {
                typeof(Facts::Advertisement),
                typeof(Facts::EntityName)
            };

        internal static readonly Type[] RulesetFactTypes =
            {
                typeof(Facts::Ruleset),
                typeof(Facts::Ruleset.AssociatedRule),
                typeof(Facts::Ruleset.DeniedRule),
                typeof(Facts::Ruleset.QuantitativeRule),
                typeof(Facts::Ruleset.RulesetProject)
            };

        public static readonly Type[] AggregateTypes =
            {
                typeof(PriceAggregates::Firm),
                typeof(PriceAggregates::Firm.FirmPosition),
                typeof(PriceAggregates::Firm.FirmAssociatedPosition),
                typeof(PriceAggregates::Firm.FirmDeniedPosition),
                typeof(PriceAggregates::Order),
                typeof(PriceAggregates::Order.OrderPeriod),
                typeof(PriceAggregates::Order.OrderPricePosition),
                typeof(PriceAggregates::Order.OrderCategoryPosition),
                typeof(PriceAggregates::Order.OrderThemePosition),
                typeof(PriceAggregates::Order.AmountControlledPosition),
                typeof(PriceAggregates::Order.EntranceControlledPosition),
                typeof(PriceAggregates::Order.ActualPrice),
                typeof(PriceAggregates::Period),
                typeof(PriceAggregates::Ruleset),
                typeof(PriceAggregates::Ruleset.AdvertisementAmountRestriction),

                typeof(AccountAggregates::Order),
                typeof(AccountAggregates::Order.DebtPermission),
                typeof(AccountAggregates::Account),
                typeof(AccountAggregates::Account.AccountPeriod),

                typeof(AdvertisementAggregates::Order),
                typeof(AdvertisementAggregates::Order.MissingAdvertisementReference),
                typeof(AdvertisementAggregates::Order.MissingOrderPositionAdvertisement),
                typeof(AdvertisementAggregates::Order.AdvertisementFailedReview),
                typeof(AdvertisementAggregates::Order.AdvertisementNotBelongToFirm),

                typeof(ConsistencyAggregates::Order),
                typeof(ConsistencyAggregates::Order.BargainSignedLaterThanOrder),
                typeof(ConsistencyAggregates::Order.HasNoAnyLegalPersonProfile),
                typeof(ConsistencyAggregates::Order.HasNoAnyPosition),
                typeof(ConsistencyAggregates::Order.InactiveReference),
                typeof(ConsistencyAggregates::Order.InvalidBillsTotal),
                typeof(ConsistencyAggregates::Order.LegalPersonProfileBargainExpired),
                typeof(ConsistencyAggregates::Order.LegalPersonProfileWarrantyExpired),
                typeof(ConsistencyAggregates::Order.MissingBargainScan),
                typeof(ConsistencyAggregates::Order.MissingBills),
                typeof(ConsistencyAggregates::Order.MissingRequiredField),
                typeof(ConsistencyAggregates::Order.MissingOrderScan),

                typeof(FirmAggregates::Firm),
                typeof(FirmAggregates::Firm.CategoryPurchase),
                typeof(FirmAggregates::Order),
                typeof(FirmAggregates::Order.FirmOrganizationUnitMismatch),
                typeof(FirmAggregates::Order.InvalidFirm),
                typeof(FirmAggregates::Order.InvalidFirmAddress),
                typeof(FirmAggregates::Order.InvalidCategory),
                typeof(FirmAggregates::Order.CategoryNotBelongsToAddress),
                typeof(FirmAggregates::Order.PartnerPosition),
                typeof(FirmAggregates::Order.PremiumPartnerPosition),
                typeof(FirmAggregates::Order.FmcgCutoutPosition),
                typeof(FirmAggregates::Order.AddressAdvertisementNonOnTheMap),
                typeof(FirmAggregates::Order.MissingValidPartnerFirmAddresses),

                typeof(ProjectAggregates::Order),
                typeof(ProjectAggregates::Order.CategoryAdvertisement),
                typeof(ProjectAggregates::Order.CostPerClickAdvertisement),
                typeof(ProjectAggregates::Project),
                typeof(ProjectAggregates::Project.Category),
                typeof(ProjectAggregates::Project.CostPerClickRestriction),
                typeof(ProjectAggregates::Project.SalesModelRestriction),
                typeof(ProjectAggregates::Project.NextRelease),

                typeof(ThemeAggregates::Theme),
                typeof(ThemeAggregates::Theme.InvalidCategory),
                typeof(ThemeAggregates::Order),
                typeof(ThemeAggregates::Order.OrderTheme),
                typeof(ThemeAggregates::Project),
                typeof(ThemeAggregates::Project.ProjectDefaultTheme),

                typeof(SystemAggregates::SystemStatus),
            };


        public static readonly Type[] MessagesTypes =
            {
                typeof(Messages::Version),
                typeof(Messages::Version.ValidationResult),
                typeof(Messages::Version.AmsState),
                typeof(Messages::Cache.ValidationResult),
            };

        public static readonly Type[] ErmMessagesTypes =
            {
                typeof(Messages::Version.ErmState),
            };

        public static readonly IReadOnlyCollection<Type> KafkaFactTypes =
            AmsFactTypes.Concat(RulesetFactTypes).ToHashSet();

        public static readonly IReadOnlyCollection<Type> AllFactTypes =
            ErmFactTypes.Concat(KafkaFactTypes).ToHashSet();

        public static readonly IReadOnlyCollection<Type> AllMessagesTypes =
            MessagesTypes.Concat(ErmMessagesTypes).ToHashSet();
    }
}
