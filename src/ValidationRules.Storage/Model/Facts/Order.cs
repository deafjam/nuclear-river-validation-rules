using System;

namespace NuClear.ValidationRules.Storage.Model.Facts
{
    public sealed class Order
    {
        public long Id { get; set; }
        public long FirmId { get; set; }

        public long DestProjectId { get; set; }

        public DateTime AgileDistributionStartDate { get; set; }
        public DateTime AgileDistributionEndPlanDate { get; set; }
        public DateTime AgileDistributionEndFactDate { get; set; }

        public DateTime SignupDate { get; set; }

        public long? LegalPersonId { get; set; }
        public long? LegalPersonProfileId { get; set; }
        public long? BranchOfficeOrganizationUnitId { get; set; }
        public long? BargainId { get; set; }
        public long? DealId { get; set; }


        public int WorkflowStep { get; set; }
        public bool IsFreeOfCharge { get; set; }
        public bool HasCurrency { get; set; }
        public bool IsSelfAds { get; set; }
        public bool IsSelfSale { get; set; }

        public static class State
        {
            public const int OnRegistration = 1;
            public const int OnTermination = 4;
            public const int Approved = 5;

            /// <summary>
            /// Состояния, означающие, что заказ влияет на лицевой счёт.
            /// </summary>
            public static readonly int[] Payable = { OnTermination, Approved };

            /// <summary>
            /// Состояния, означающие, что заказ размещается.
            /// </summary>
            public static readonly int[] Committed = { OnTermination, Approved };
        }
    }
}
