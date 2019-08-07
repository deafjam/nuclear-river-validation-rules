using System;
using System.Collections.Generic;
using System.Linq;

namespace NuClear.ValidationRules.Replication.PriceRules.Aggregates
{
    public struct TimeRange
    {
        public DateTime Start { get; }
        public DateTime End { get; }

        public TimeRange(DateTime start, DateTime end)
        {
            if (start >= end)
            {
                throw new ArgumentException("Invalid time range");
            }

            (Start, End) = (start, end);
        }

        public bool Equals(TimeRange other) => Start.Equals(other.Start) && End.Equals(other.End);

        public override bool Equals(object obj) => obj is TimeRange other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start.GetHashCode() * 397) ^ End.GetHashCode();
            }
        }
    }

    public static class TimeRangeExtensions
    {
        public static IReadOnlyCollection<TimeRange> Merge(this IEnumerable<TimeRange> ranges)
        {
            return ranges.Aggregate(new HashSet<TimeRange>(), (set, range) =>
            {
                var overlaps = set.Where(x => Overlaps(range, x)).ToList();
                if (overlaps.Count != 0)
                {
                    var mergedStart = range.Start;
                    var mergedEnd = range.End;
                    foreach (var overlap in overlaps)
                    {
                        mergedStart = overlap.Start < mergedStart ? overlap.Start : mergedStart;
                        mergedEnd = overlap.End > mergedEnd ? overlap.End : mergedEnd;
                    }

                    set.RemoveWhere(x => overlaps.Contains(x));
                    set.Add(new TimeRange(mergedStart, mergedEnd));
                }
                else
                {
                    set.Add(range);
                }

                return set;
            });

            bool Overlaps(TimeRange one, TimeRange other) => one.Start <= other.End && other.Start <= one.End;
        }
    }
}