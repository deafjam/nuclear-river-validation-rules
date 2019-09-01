using System;

namespace NuClear.ValidationRules.Storage.Model.Facts
{
    // правила загружаются из GoldMine для проекта на определённую дату
    // новые правила отменяют действие всех предыдущих правил по всем рубрикам
    public sealed class SalesModelCategoryRestriction
    {
        public long ProjectId { get; set; }
        public DateTime Start { get; set; }
        
        public long CategoryId { get; set; }
        public int SalesModel { get; set; }
    }
    
    public sealed class CostPerClickCategoryRestriction
    {
        public long ProjectId { get; set; }
        public DateTime Start { get; set; }
        
        public long CategoryId { get; set; }
        public decimal MinCostPerClick { get; set; }
    }
}