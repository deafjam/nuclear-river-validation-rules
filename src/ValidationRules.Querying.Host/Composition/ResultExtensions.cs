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

        public static string ExtractPeriod(this IReadOnlyDictionary<string, string> extra)
        {
            var start = DateTime.Parse(extra["start"]);
            var end = DateTime.Parse(extra["end"]);
                
            var period = start.Month == end.AddSeconds(-1).Month
                ? start.ToString("MMMM yyyy")
                : $"{start:MMMM yyyy} - {end:MMMM yyyy}";

            return period;
        }

        public static IReadOnlyDictionary<string, string> UnionPeriod(this IEnumerable<Message> messages, IReadOnlyDictionary<string, string> dictionary)
        {
            var (start, end) = messages.Aggregate((start: DateTime.MaxValue, end: DateTime.MinValue), (period, message) =>
            {
                var messageStart = DateTime.Parse(message.Extra["start"]);
                if (messageStart < period.start)
                {
                    period.start = messageStart;
                }

                var messageEnd = DateTime.Parse(message.Extra["end"]);
                if (messageEnd > period.end)
                {
                    period.end = messageEnd;
                }

                return period;
            });

            var result = dictionary.ToDictionary(x => x.Key, x => x.Value);
            result["start"] = start.ToString(CultureInfo.CurrentCulture);
            result["end"] = end.ToString(CultureInfo.CurrentCulture);

            return result;
        }

        public static string ExtractPeriods(this IReadOnlyDictionary<string, string> extra)
        {
            var periods = JsonConvert.DeserializeObject<IReadOnlyCollection<DateTime>>(extra["periods"])
                .OrderBy(x => x)
                .Select(x => x.ToString("MMMM yyyy"));

            return string.Join(", ", periods);
        }
       
        public static IReadOnlyDictionary<string, string> UnionPeriods(this IEnumerable<Message> messages, IReadOnlyDictionary<string, string> dictionary)
        {
            var result = dictionary.ToDictionary(x => x.Key, x => x.Value);
            result["periods"] = JsonConvert.SerializeObject(messages.Select(x => x.Extra).SelectMany(MonthlySplit).ToHashSet());

            return result;
        }

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