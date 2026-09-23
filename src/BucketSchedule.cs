using System;
using System.Collections.Generic;

namespace MouseClickCounter
{
    public sealed class TimeBucket
    {
        public string Start { get; private set; }
        public string End { get; private set; }
        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; private set; }

        public TimeBucket(string start, string end)
        {
            Start = start;
            End = end;
            StartTime = TimeSpan.Parse(start);
            // "24:00" is stored as exclusive end-of-day
            EndTime = end == "24:00" ? TimeSpan.FromDays(1) : TimeSpan.Parse(end);
        }

        public bool Contains(TimeSpan timeOfDay)
        {
            return timeOfDay >= StartTime && timeOfDay < EndTime;
        }

        public string Key
        {
            get { return Start + "-" + End; }
        }
    }

    public static class BucketSchedule
    {
        private static readonly TimeBucket[] WeekdayBuckets = BuildWeekday();
        private static readonly TimeBucket[] WeekendBuckets = new TimeBucket[]
        {
            new TimeBucket("00:00", "12:00"),
            new TimeBucket("12:00", "24:00")
        };

        private static TimeBucket[] BuildWeekday()
        {
            var list = new List<TimeBucket>();
            list.Add(new TimeBucket("00:00", "09:30"));

            // Nine :30-aligned hourly slots from 09:30 to 18:30
            var starts = new string[]
            {
                "09:30", "10:30", "11:30", "12:30", "13:30",
                "14:30", "15:30", "16:30", "17:30"
            };
            var ends = new string[]
            {
                "10:30", "11:30", "12:30", "13:30", "14:30",
                "15:30", "16:30", "17:30", "18:30"
            };
            for (int i = 0; i < starts.Length; i++)
            {
                list.Add(new TimeBucket(starts[i], ends[i]));
            }

            list.Add(new TimeBucket("18:30", "24:00"));
            return list.ToArray();
        }

        public static bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }

        public static TimeBucket[] GetBucketsFor(DateTime date)
        {
            return IsWeekend(date) ? WeekendBuckets : WeekdayBuckets;
        }

        public static TimeBucket Resolve(DateTime when)
        {
            var buckets = GetBucketsFor(when);
            var tod = when.TimeOfDay;
            for (int i = 0; i < buckets.Length; i++)
            {
                if (buckets[i].Contains(tod))
                {
                    return buckets[i];
                }
            }
            // Safety: last bucket
            return buckets[buckets.Length - 1];
        }

        public static string DayType(DateTime date)
        {
            return IsWeekend(date) ? "weekend" : "weekday";
        }
    }
}
