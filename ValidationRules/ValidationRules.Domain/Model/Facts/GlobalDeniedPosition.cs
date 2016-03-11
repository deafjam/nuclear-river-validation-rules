namespace NuClear.ValidationRules.Domain.Model.Facts
{
    public sealed class GlobalDeniedPosition : IErmFactObject
    {
        public long Id { get; set; }
        public long RulesetId { get; set; }
        public long DeniedPositionId { get; set; }
        public long PrincipalPositionId { get; set; }
        public int ObjectBindingType { get; set; }
    }
}