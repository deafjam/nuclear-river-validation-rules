﻿using System.Collections.Generic;
using System.Linq;

using NuClear.ValidationRules.Storage.Model.Messages;

namespace ValidationRules.Replication.SingleCheck.Tests.RiverService
{
    public static class RiverToErmMappingExtensions
    {
        private static readonly IReadOnlyDictionary<MessageTypeCode, int> RiverToErmRuleCodeMapping
            = new Dictionary<MessageTypeCode, int>
                    {
                            { MessageTypeCode.MaximumAdvertisementAmount, 26 },
                            { MessageTypeCode.MinimalAdvertisementRestrictionShouldBeSpecified, 26 },
                            { MessageTypeCode.OrderPositionsShouldCorrespontToActualPrice, 15 },
                            { MessageTypeCode.OrderPositionCorrespontToInactivePosition, 15 },
                            { MessageTypeCode.OrderPositionShouldCorrespontToActualPrice, 15 },
                            { MessageTypeCode.MinimumAdvertisementAmount, 26 },
                            { MessageTypeCode.AssociatedPositionsGroupCount, 6 },
                            { MessageTypeCode.DeniedPositionsCheck, 6 },
                            { MessageTypeCode.AssociatedPositionWithoutPrincipal, 6 },
                            { MessageTypeCode.LinkedObjectsMissedInPrincipals, 6 },
                            { MessageTypeCode.ConflictingPrincipalPosition, 6 },
                            { MessageTypeCode.SatisfiedPrincipalPositionDifferentOrder, 6 },
                            { MessageTypeCode.AdvertisementCountPerThemeShouldBeLimited, 44 },
                            { MessageTypeCode.AdvertisementCountPerCategoryShouldBeLimited, 31 },
                            { MessageTypeCode.AccountShouldExist, 3 },
                            { MessageTypeCode.LockShouldNotExist, 13 },
                            { MessageTypeCode.AccountBalanceShouldBePositive, 20 },
                            { MessageTypeCode.OrderBeginDistrubutionShouldBeFirstDayOfMonth, 9 },
                            { MessageTypeCode.OrderEndDistrubutionShouldBeLastSecondOfMonth, 9 },
                            { MessageTypeCode.LegalPersonProfileBargainShouldNotBeExpired, 25 },
                            { MessageTypeCode.LegalPersonProfileWarrantyShouldNotBeExpired, 24 },
                            { MessageTypeCode.BillsPeriodShouldMatchOrder, 7 },
                            { MessageTypeCode.OrderShouldNotBeSignedBeforeBargain, 1 },
                            { MessageTypeCode.LegalPersonShouldHaveAtLeastOneProfile, 23 },
                            { MessageTypeCode.OrderShouldHaveAtLeastOnePosition, 14 },
                            { MessageTypeCode.OrderScanShouldPresent, 16 },
                            { MessageTypeCode.BargainScanShouldPresent, 16 },
                            { MessageTypeCode.OrderRequiredFieldsShouldBeSpecified, 18 },
                            { MessageTypeCode.LinkedFirmAddressShouldBeValid, 12 },
                            { MessageTypeCode.LinkedCategoryFirmAddressShouldBeValid, 12 },
                            { MessageTypeCode.LinkedCategoryShouldBelongToFirm, 12 },
                            { MessageTypeCode.LinkedCategoryAsterixMayBelongToFirm, 12 },
                            { MessageTypeCode.LinkedCategoryShouldBeActive, 12 },
                            { MessageTypeCode.LinkedFirmShouldBeValid, 11 },
                            { MessageTypeCode.BillsSumShouldMatchOrder, 7 },
                            { MessageTypeCode.BillsShouldBeCreated, 7 },
                            { MessageTypeCode.FirmAndOrderShouldBelongTheSameOrganizationUnit, 10 },
                            { MessageTypeCode.FirmShouldHaveLimitedCategoryCount, 32 },
                            { MessageTypeCode.FirmWithSpecialCategoryShouldHaveSpecialPurchases, 29 },
                            { MessageTypeCode.OrderPositionAdvertisementMustHaveAdvertisement, 22 },
                            { MessageTypeCode.OrderPositionAdvertisementMustBeCreated, 22 },
                            { MessageTypeCode.OrderPositionMustNotReferenceDeletedAdvertisement, 22 },
                            { MessageTypeCode.AdvertisementMustBelongToFirm, 22 },
                            { MessageTypeCode.OrderMustNotContainDummyAdvertisement, 47 },
                            { MessageTypeCode.OrderMustHaveAdvertisement, 22 },
                            { MessageTypeCode.AdvertisementElementMustPassReview, 22 },
                            { MessageTypeCode.WhiteListAdvertisementMustPresent, 21 },
                            { MessageTypeCode.WhiteListAdvertisementMayPresent, 21 },
                            { MessageTypeCode.ProjectMustContainCostPerClickMinimumRestriction, 49 },
                            { MessageTypeCode.OrderMustUseCategoriesOnlyAvailableInProject, 7 },
                            { MessageTypeCode.OrderMustNotIncludeReleasedPeriod, 17 },
                            { MessageTypeCode.OrderPositionCostPerClickMustNotBeLessMinimum, 48 },
                            { MessageTypeCode.FirmAddressMustBeLocatedOnTheMap, 34 },
                            { MessageTypeCode.ThemeCategoryMustBeActiveAndNotDeleted, 43 },
                            { MessageTypeCode.ThemePeriodMustContainOrderPeriod, 42 },
                            { MessageTypeCode.DefaultThemeMustHaveOnlySelfAds, 41 },
                            { MessageTypeCode.DefaultThemeMustBeExactlyOne, 40 },
                            { MessageTypeCode.OrderPositionCostPerClickMustBeSpecified, 46 },
                            { MessageTypeCode.OrderPositionSalesModelMustMatchCategorySalesModel, 45 },
                            { MessageTypeCode.FirmWithSelfAdvMustHaveOnlyDesktopOrIndependentPositions, 36 },
                            { MessageTypeCode.OrderMustHaveActiveDeal, 51 },
                            { MessageTypeCode.OrderMustHaveActiveLegalEntities, 52 },
                            { MessageTypeCode.AdvantageousPurchasesBannerMustBeSoldInTheSameCategory, 38 },
                            { MessageTypeCode.CouponMustBeSoldOnceAtTime, 35 },
                            { MessageTypeCode.OrderCouponPeriodMustNotBeLessFiveDays, 2 },
                            { MessageTypeCode.AdvertisementWebsiteShouldNotBeFirmWebsite, 27 },
                            { MessageTypeCode.FirmWithSpecialCategoryShouldHaveSpecialPurchasesOrder, 29 },
                            { MessageTypeCode.OrderCouponPeriodMustBeInRelease, 2 },
                    }.Where(x => x.Value != 0).ToDictionary(x => x.Key, x => x.Value);

        public static int ToErmRuleCode(this int riverMessageTypeCode)
            => RiverToErmRuleCodeMapping[(MessageTypeCode)riverMessageTypeCode];
    }
}