using System;
using System.Collections.Generic;
using System.Linq;
using NuClear.Replication.Core;
using NuClear.Replication.Core.DataObjects;
using NuClear.StateInitialization.Core.Commands;
using NuClear.StateInitialization.Core.DataObjects;
using NuClear.StateInitialization.Core.Factories;
using NuClear.ValidationRules.Storage;
using NuClear.ValidationRules.Storage.Connections;

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
    public sealed class DataObjectTypesProviderFactory : IDataObjectTypesProviderFactory
    {
        public static IReadOnlyCollection<Type> AllSourcesFactTypes => ErmFactTypes.Concat(AmsFactTypes).Concat(RulesetFactTypes).ToHashSet();

        internal static readonly Type[] ErmFactTypes =
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
                typeof(Facts::OrderItem),
                typeof(Facts::OrderPosition),
                typeof(Facts::OrderPositionAdvertisement),
                typeof(Facts::OrderPositionCostPerClick),
                typeof(Facts::OrderScanFile),
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
                typeof(ConsistencyAggregates::Order.InvalidFirmAddress),
                typeof(ConsistencyAggregates::Order.InvalidCategory),
                typeof(ConsistencyAggregates::Order.CategoryNotBelongsToAddress),
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
                typeof(ConsistencyAggregates::Order.MissingValidPartnerFirmAddresses),

                typeof(FirmAggregates::Firm),
                typeof(FirmAggregates::Firm.CategoryPurchase),
                typeof(FirmAggregates::Order),
                typeof(FirmAggregates::Order.FirmOrganizationUnitMismatch),
                typeof(FirmAggregates::Order.InvalidFirm),
                typeof(FirmAggregates::Order.PartnerPosition),
                typeof(FirmAggregates::Order.PremiumPartnerPosition),
                typeof(FirmAggregates::Order.FmcgCutoutPosition),

                typeof(ProjectAggregates::Order),
                typeof(ProjectAggregates::Order.AddressAdvertisementNonOnTheMap),
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

        
        internal static readonly Type[] MessagesTypes =
            {
                typeof(Messages::Version),
                typeof(Messages::Version.ValidationResult),
                typeof(Messages::Version.AmsState),
                typeof(Messages::Cache.ValidationResult),
            };

        internal static readonly Type[] ErmMessagesTypes =
        {
            typeof(Messages::Version.ErmState),
        };

        public static readonly IReadOnlyCollection<Type> AllMessagesTypes = MessagesTypes.Concat(ErmMessagesTypes).ToList();

        public IDataObjectTypesProvider Create(ReplicateInBulkCommand command)
        {
            if (command.TargetStorageDescriptor.MappingSchema == Schema.Facts)
            {
                if (command.SourceStorageDescriptor.ConnectionStringIdentity is AmsConnectionStringIdentity)
                {
                    return new DataObjectTypesProvider(AmsFactTypes);
                }

                if (command.SourceStorageDescriptor.ConnectionStringIdentity is RulesetConnectionStringIdentity)
                {
                    return new DataObjectTypesProvider(RulesetFactTypes);
                }

                return new CommandRegardlessDataObjectTypesProvider(ErmFactTypes);
            }

            if (command.TargetStorageDescriptor.MappingSchema == Schema.Aggregates)
            {
                return new CommandRegardlessDataObjectTypesProvider(AggregateTypes);
            }

            if (command.TargetStorageDescriptor.MappingSchema == Schema.Messages)
            {
                if (command.SourceStorageDescriptor.MappingSchema == Schema.Erm)
                {
                    return new CommandRegardlessDataObjectTypesProvider(ErmMessagesTypes);    
                }
                
                return new CommandRegardlessDataObjectTypesProvider(MessagesTypes);
            }

            throw new ArgumentException($"Instance of type IDataObjectTypesProvider cannot be created for connection string name {command.TargetStorageDescriptor.MappingSchema}");
        }

        // CommandRegardlessDataObjectTypesProvider - он internal в StateInitiallization.Core, пришлось запилить вот это
        internal sealed class DataObjectTypesProvider : IDataObjectTypesProvider
        {
            public IReadOnlyCollection<Type> DataObjectTypes { get; }

            public DataObjectTypesProvider(IReadOnlyCollection<Type> dataObjectTypes)
            {
                DataObjectTypes = dataObjectTypes;
            }

            public IReadOnlyCollection<Type> Get<TCommand>() where TCommand : ICommand => throw new NotImplementedException();
        }
    }
}
