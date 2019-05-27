using System;

namespace NuClear.ValidationRules.Storage.Model.Erm
{
    public sealed class SalesModelCategoryRestriction
    {
        public long ProjectId { get; set; }
        public long CategoryId { get; set; }
        public DateTime BeginningDate { get; set; }
        public int SalesModel { get; set; }
    }
}