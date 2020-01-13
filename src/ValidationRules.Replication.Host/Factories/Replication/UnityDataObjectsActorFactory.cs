using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Practices.Unity;

using NuClear.Replication.Core;
using NuClear.Replication.Core.Actors;
using NuClear.ValidationRules.Storage.Model.Facts;

namespace NuClear.ValidationRules.Replication.Host.Factories.Replication
{
    public sealed class UnityDataObjectsActorFactory : IDataObjectsActorFactory
    {
        private readonly IUnityContainer _unityContainer;

        private static readonly HashSet<Type> SyncDataObjectsActorTypes = new HashSet<Type>
        {
            typeof(Account),
            typeof(AccountDetail),
            typeof(Bargain),
            typeof(BargainScanFile),
            typeof(Bill),
            typeof(BranchOffice),
            typeof(BranchOfficeOrganizationUnit),
            typeof(Category),
            typeof(CategoryOrganizationUnit),
            typeof(CostPerClickCategoryRestriction),
            typeof(Deal),
            typeof(Firm),
            typeof(FirmInactive),
            typeof(FirmAddress),
            typeof(FirmAddressInactive),
            typeof(FirmAddressCategory),
            typeof(LegalPerson),
            typeof(LegalPersonProfile),
            typeof(NomenclatureCategory),
            typeof(Order),
            typeof(OrderWorkflow),
            typeof(OrderConsistency),
            typeof(OrderItem),
            typeof(OrderPosition),
            typeof(OrderPositionAdvertisement),
            typeof(OrderPositionCostPerClick),
            typeof(OrderScanFile),
            typeof(Position),
            typeof(PositionChild),
            typeof(Price),
            typeof(PricePosition),
            typeof(Project),
            typeof(ReleaseInfo),
            typeof(ReleaseWithdrawal),
            typeof(SalesModelCategoryRestriction),
            typeof(Theme),
            typeof(ThemeCategory),
            typeof(ThemeOrganizationUnit),
            typeof(UnlimitedOrder),
        };

        private static readonly HashSet<Type> ReplaceDataObjectsActorTypes = new HashSet<Type>
        {
            typeof(Advertisement),
            typeof(Ruleset),
            typeof(Ruleset.AssociatedRule),
            typeof(Ruleset.DeniedRule),
            typeof(Ruleset.QuantitativeRule),
            typeof(Ruleset.RulesetProject)
        };

        public UnityDataObjectsActorFactory(IUnityContainer unityContainer) => _unityContainer = unityContainer;

        public IReadOnlyCollection<IActor> Create(IReadOnlyCollection<Type> dataObjectTypes)
        {
            var actorTypes = dataObjectTypes.Select(x =>
            {
                Type actorTypeDefinition;
                if (SyncDataObjectsActorTypes.Contains(x))
                {
                    actorTypeDefinition = typeof(SyncDataObjectsActor<>);
                } else if (ReplaceDataObjectsActorTypes.Contains(x))
                {
                    actorTypeDefinition = typeof(ReplaceDataObjectsActor<>);
                } else throw new ArgumentException($"Unkown data object type {x.FullName}");

                return actorTypeDefinition.MakeGenericType(x);
            });

            var actors = actorTypes.Select(x => (IActor)_unityContainer.Resolve(x)).ToList();
            return actors;
        }
    }
}