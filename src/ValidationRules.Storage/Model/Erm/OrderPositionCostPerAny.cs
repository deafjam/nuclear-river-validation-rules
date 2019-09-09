namespace NuClear.ValidationRules.Storage.Model.Erm
{
    public sealed class OrderPositionCostPerAny
    {
        public long OrderPositionId { get; set; }
        public long? CategoryId { get; set; }
        public long BidIndex { get; set; }
        public decimal Amount { get; set; }
    }
}