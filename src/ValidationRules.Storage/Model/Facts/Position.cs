using System.Collections.Generic;

namespace NuClear.ValidationRules.Storage.Model.Facts
{
    public sealed class Position
    {
        public const long CategoryCodeAdvertisementInCategory = 38; // Объявление в рубрике (Объявление под списком выдачи)
        public const long CategoryCodeBasicPackage = 303; // пакет "Базовый" ???
        public const long CategoryCodeMediaContextBanner = 395122163464046280; // Баннер в поисковой выдаче (онлайн-версия)
        public const long CategoryCodeContextBanner = 809065011136692318; // Баннер в поисковой выдаче (мобильная версия)
        public const long CategoryCodeAdvertisementLink = 11; // Рекламная ссылка
        public const long CategoryCodeBargains2Gis = 14; // Выгодные покупки с 2ГИС
        public const long CategoryCodeAddressComment = 26; // Комментарий к адресу
        public const long CategoryCodePartnerAdvertising = 809065011136692320; // Реклама компании в карточках партнёров (партнеры)
        public const long CategoryCodePremiumPartnerAdvertising = 809065011136692321; // Реклама компании в карточках партнёров (партнеры FMCG)_
        public const long CategoryCodePremiumPartnerAdvertising2 = 809065011136692333; // Реклама компании в карточках партнёров (партнеры FMCG)
        public const long CategoryCodePartnerAdvertisingAddress = 809065011136692326; // Реклама компании в карточках партнёров (адреса)
        public const long CategoryCodeLogo = 448239782219049217; // Логотипы на карте_Online_old
        public const long CategoryCodeLogo2 = 809065011136692327; // Логотипы на карте

        public const int BindingObjectTypeCategoryMultipleAsterisk = 1;
        public const int BindingObjectTypeAddressMultiple = 35;

        public const int PositionsGroupMedia = 1;

        public const int ContentSalesWithoutContent = 1;
        public const int ContentSalesContentIsNotRequired = 2;

        public static readonly IReadOnlyCollection<long> CategoryCodesAllowNotLocatedOnTheMap = new[]
        {
            CategoryCodeAdvertisementLink,
            CategoryCodeBargains2Gis,
            CategoryCodeAddressComment,
            CategoryCodePartnerAdvertisingAddress
        };

        public static readonly IReadOnlyCollection<long> CategoryCodesCategoryCodePremiumPartnerAdvertising = new[]
        {
            CategoryCodePremiumPartnerAdvertising,
            CategoryCodePremiumPartnerAdvertising2
        };

        /// <summary>
        /// Категории номенклатуры, для которых допускается несовпадение фирмы заказа и фирмы адреса привязки (продажи в чужие карточки)
        /// </summary>
        public static readonly IReadOnlyCollection<long> CategoryCodesAllowFirmMismatch = new[]
        {
            CategoryCodePartnerAdvertising,
            CategoryCodePremiumPartnerAdvertising,
            CategoryCodePremiumPartnerAdvertising2,
            CategoryCodePartnerAdvertisingAddress,
        };

        public static readonly IReadOnlyCollection<long> CategoryCodesPoiAddressCheck = new[]
        {
            CategoryCodeLogo,
            CategoryCodeLogo2,
        };

        public long Id { get; set; }

        public int BindingObjectType { get; set; }
        public int SalesModel { get; set; }
        public int PositionsGroup { get; set; }

        public bool IsCompositionOptional { get; set; }
        public int ContentSales { get; set; }

        public bool IsControlledByAmount { get; set; }

        public long CategoryCode { get; set; }

        public bool IsDeleted { get; set; }
    }
}
