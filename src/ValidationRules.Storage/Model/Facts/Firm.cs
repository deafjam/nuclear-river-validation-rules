namespace NuClear.ValidationRules.Storage.Model.Facts
{
    // Таблица фирм очень большая
    // Неактивных фирм больше чем активных
    // Для неактивных фирм имеет смысл только одна проверка (LinkedFirmShouldBeValid)
    // Для того, чтобы облегчить жизнь всем остальных проверкам, мы разбили таблицу фирм на две

    public sealed class Firm
    {
        public long Id { get; set; }
        public long ProjectId { get; set; }
    }

    public sealed class FirmInactive
    {
        public long Id { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsClosedForAscertainment { get; set; }
    }
}