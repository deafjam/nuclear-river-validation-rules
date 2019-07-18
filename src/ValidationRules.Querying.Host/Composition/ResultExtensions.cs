using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using NuClear.ValidationRules.Storage.Model.Aggregates.ConsistencyRules;
using NuClear.ValidationRules.Storage.Model.Aggregates.FirmRules;
using Order = NuClear.ValidationRules.Storage.Model.Aggregates.AdvertisementRules.Order;

namespace NuClear.ValidationRules.Querying.Host.Composition
{
    public static class ResultExtensions
    {
        public static AccountBalanceMessageDto ReadAccountBalanceMessage(this IReadOnlyDictionary<string, string> message)
        {
            return new AccountBalanceMessageDto
                {
                    Available = decimal.Parse(message["available"], CultureInfo.InvariantCulture),
                    Planned = decimal.Parse(message["planned"], CultureInfo.InvariantCulture),
                };
        }

        public static AdvertisementCountDto ReadAdvertisementCountMessage(this IReadOnlyDictionary<string, string> message)
        {
            return new AdvertisementCountDto
            {
                Min = int.Parse(message["min"], CultureInfo.InvariantCulture),
                Max = int.Parse(message["max"], CultureInfo.InvariantCulture),
                Count = int.Parse(message["count"], CultureInfo.InvariantCulture),
                Start = DateTime.Parse(message["start"], CultureInfo.InvariantCulture),
                End = DateTime.Parse(message["end"], CultureInfo.InvariantCulture),
            };
        }

        public static OversalesDto ReadOversalesMessage(this IReadOnlyDictionary<string, string> message)
        {
            return new OversalesDto
                {
                    Max = int.Parse(message["max"], CultureInfo.InvariantCulture),
                    Count = int.Parse(message["count"], CultureInfo.InvariantCulture),
                };
        }

        public static InvalidFirmAddressState ReadFirmAddressState(this IReadOnlyDictionary<string, string> message)
        {
            return (InvalidFirmAddressState)int.Parse(message["invalidFirmAddressState"], CultureInfo.InvariantCulture);
        }

        public static CategoryCountDto ReadCategoryCount(this IReadOnlyDictionary<string, string> message)
        {
            return new CategoryCountDto
            {
                Actual = int.Parse(message["count"], CultureInfo.InvariantCulture),
                Allowed = int.Parse(message["allowed"], CultureInfo.InvariantCulture),
            };
        }

        public static InvalidFirmState ReadFirmState(this IReadOnlyDictionary<string, string> message)
        {
            return (InvalidFirmState)int.Parse(message["invalidFirmState"], CultureInfo.InvariantCulture);
        }

        public static OrderRequiredFieldsDto ReadOrderRequiredFieldsMessage(this IReadOnlyDictionary<string, string> message)
        {
            return new OrderRequiredFieldsDto
            {
                LegalPerson = bool.Parse(message["legalPerson"]),
                LegalPersonProfile = bool.Parse(message["legalPersonProfile"]),
                BranchOfficeOrganizationUnit = bool.Parse(message["branchOfficeOrganizationUnit"]),
                Currency = bool.Parse(message["currency"]),
            };
        }

        public static OrderInactiveFieldsDto ReadOrderInactiveFieldsMessage(this IReadOnlyDictionary<string, string> message)
        {
            return new OrderInactiveFieldsDto
            {
                LegalPerson = bool.Parse(message["legalPerson"]),
                LegalPersonProfile = bool.Parse(message["legalPersonProfile"]),
                BranchOfficeOrganizationUnit = bool.Parse(message["branchOfficeOrganizationUnit"]),
                BranchOffice = bool.Parse(message["branchOffice"]),
            };
        }

        public static int ReadProjectThemeCount(this IReadOnlyDictionary<string, string> message)
        {
            return int.Parse(message["themeCount"], CultureInfo.InvariantCulture);
        }

        public static DealState ReadDealState(this IReadOnlyDictionary<string, string> message)
        {
            return (DealState)int.Parse(message["state"]);
        }

        public static DateTime ReadStartDate(this IReadOnlyDictionary<string, string> message)
        {
            return DateTime.Parse(message["start"]);
        }

        public static Order.AdvertisementReviewState ReadAdvertisementReviewState(this IReadOnlyDictionary<string, string> message)
        {
            return (Order.AdvertisementReviewState)int.Parse(message["reviewState"], CultureInfo.InvariantCulture);
        }

        public static IReadOnlyCollection<DateTime> ExtractPeriods(this IReadOnlyDictionary<string, string> extra)
        {
            return FromString(extra["periods"]);
        }

        public static Dictionary<string, string> StorePeriods(this IReadOnlyCollection<Message> messages, Dictionary<string, string> extra)
        {
            extra.Add("periods", messages.Select(x => x.Extra).SelectMany(MonthlySplit).ToHashSet().ConvertToString());
            return extra;
        }

        private static IReadOnlyCollection<DateTime> FromString(string str)
            => JsonConvert.DeserializeObject<DateTime[]>(str);

        private static IEnumerable<DateTime> MonthlySplit(IReadOnlyDictionary<string, string> extra)
            => MonthlySplit(extra["start"], extra["end"]);

        private static IEnumerable<DateTime> MonthlySplit(string start, string end)
            => MonthlySplit(DateTime.Parse(start), DateTime.Parse(end));

        private static IEnumerable<DateTime> MonthlySplit(DateTime start, DateTime end)
        {
            for (var x = start; x < end; x = x.AddMonths(1))
            {
                yield return new DateTime(x.Year, x.Month, 1);
            }
        }

        private static string ConvertToString(this IEnumerable<DateTime> periods)
            => JsonConvert.SerializeObject(periods);
        
        public sealed class CategoryCountDto
        {
            public int Allowed { get; set; }
            public int Actual { get; set; }
        }

        public sealed class AccountBalanceMessageDto
        {
            public decimal Available { get; set; }
            public decimal Planned { get; set; }
        }

        public sealed class AdvertisementCountDto
        {
            public int Min { get; set; }
            public int Max { get; set; }
            public int Count { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
        }

        public sealed class OversalesDto
        {
            public int Max { get; set; }
            public int Count { get; set; }
        }

        public sealed class OrderInactiveFieldsDto
        {
            public bool LegalPerson { get; set; }
            public bool LegalPersonProfile { get; set; }
            public bool BranchOfficeOrganizationUnit { get; set; }
            public bool BranchOffice { get; set; }
        }

        public sealed class OrderRequiredFieldsDto
        {
            public bool LegalPerson { get; set; }
            public bool LegalPersonProfile { get; set; }
            public bool BranchOfficeOrganizationUnit { get; set; }
            public bool Currency { get; set; }
        }
    }
}