using System;

namespace NuClear.ValidationRules.Storage.Model.Erm
{
    public sealed class UseCaseTrackingEvent
    {
        public const int Committed = 3; 
        
        public Guid UseCaseId { get; set; }
        public int EventType { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}