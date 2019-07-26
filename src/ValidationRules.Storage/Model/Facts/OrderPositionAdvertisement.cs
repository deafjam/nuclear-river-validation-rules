namespace NuClear.ValidationRules.Storage.Model.Facts
{
    public sealed class OrderPositionAdvertisement
    {
        public long OrderPositionId { get; set; }
        public long OrderId { get; set; }

        public long PositionId { get; set; }

        public long? FirmAddressId { get; set; }
        public long? CategoryId { get; set; }
        public long? AdvertisementId { get; set; }
        public long? ThemeId { get; set; }
    }
}
