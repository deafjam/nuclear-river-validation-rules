using System;

namespace NuClear.ValidationRules.Storage.Model.Facts
{
    // часть заказа, задействованная в контекстах Consistency и частично Account
    public sealed class OrderConsistency
    {
        public long Id { get; set; }
        
        public DateTime SignupDate { get; set; }
        public long? LegalPersonId { get; set; }
        public long? LegalPersonProfileId { get; set; }
        public long? BranchOfficeOrganizationUnitId { get; set; }
        public long? BargainId { get; set; }
        public long? DealId { get; set; }
        
        public bool IsFreeOfCharge { get; set; }
        public bool HasCurrency { get; set; }
    }
}