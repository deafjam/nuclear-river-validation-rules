using System;

namespace NuClear.ValidationRules.Storage.Model.Facts
{
    public class SalesModelCategoryRestriction
    {
        public long ProjectId { get; set; }
        public long CategoryId { get; set; }
        public DateTime Start { get; set; }
        public int SalesModel { get; set; }
    }
}