namespace NuClear.ValidationRules.Storage.Model.Facts
{
    // часть заказа, отвечающая за прохождение закза по воркфле
    // контекстам Advertisement, Consistency, Theme неважно где заказ сейчас по форкфле
    // чтобы эти контексты не пересчитывались, была выделена отдельная часть заказа
    public sealed class OrderWorkflow
    {
        public long Id { get; set; }
        public int Step { get; set; }
    }
    
    public static class OrderWorkflowStep
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