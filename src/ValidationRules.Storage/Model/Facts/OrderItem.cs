namespace NuClear.ValidationRules.Storage.Model.Facts
{
    // бизнес-смысл: облегчённый OrderPositionAdvertisement
    // прямая связь заказа со всеми его объектами привязки
    public sealed class OrderItem
    {
        public long OrderId { get; set; }
        public long OrderPositionId { get; set; }
        public long PackagePositionId { get; set; }
        public long ItemPositionId { get; set; }
        public long? FirmAddressId { get; set; }
        public long? CategoryId { get; set; }
    }
}