using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MouseClickCounter
{
    public sealed class ClickCounts
    {
        public long Left;
        public long Right;
        public long Middle;
        public long Scrolls;
        public long Keys;
        public long Spaces;
        public long Enters;
        public long Backspaces;

        public long Total
        {
            get { return Left + Right + Middle; }
        }

        public void Add(MouseButton button)
        {
            if (button == MouseButton.Left) Left++;
            else if (button == MouseButton.Right) Right++;
            else if (button == MouseButton.Middle) Middle++;
        }

        public void AddScroll()
        {
            Scrolls++;
        }

        public void Add(KeyKind kind)
        {
            Keys++;
            if (kind == KeyKind.Space) Spaces++;
            else if (kind == KeyKind.Enter) Enters++;
            else if (kind == KeyKind.Backspace) Backspaces++;
        }

        public void AddFrom(ClickCounts other)
        {
            Left += other.Left;
            Right += other.Right;
            Middle += other.Middle;
            Scrolls += other.Scrolls;
            Keys += other.Keys;
            Spaces += other.Spaces;
            Enters += other.Enters;
            Backspaces += other.Backspaces;
        }
    }

    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }

    public sealed class BucketRecord
    {
        public string Start;
        public string End;
        public ClickCounts Counts = new ClickCounts();
    }

    public sealed class DayRecord
    {
        public string Date; // yyyy-MM-dd
        public string DayType; // weekday | weekend
        public ClickCounts Daily = new ClickCounts();
        public List<BucketRecord> Buckets = new List<BucketRecord>();

        public static DayRecord CreateEmpty(DateTime date)
        {
            var record = new DayRecord();
            record.Date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            record.DayType = BucketSchedule.DayType(date);
            record.Daily = new ClickCounts();
            record.Buckets = new List<BucketRecord>();

            var schedule = BucketSchedule.GetBucketsFor(date);
            for (int i = 0; i < schedule.Length; i++)
            {
                var b = new BucketRecord();
                b.Start = schedule[i].Start;
                b.End = schedule[i].End;
                b.Counts = new ClickCounts();
                record.Buckets.Add(b);
            }
            return record;
        }

        public void RecordClick(DateTime when, MouseButton button)
        {
            Daily.Add(button);
            AddToBucket(when).Add(button);
        }

        public void RecordKey(DateTime when, KeyKind kind)
        {
            Daily.Add(kind);
            AddToBucket(when).Add(kind);
        }

        public void RecordScroll(DateTime when)
        {
            Daily.AddScroll();
            AddToBucket(when).AddScroll();
        }

        private ClickCounts AddToBucket(DateTime when)
        {
            var bucket = BucketSchedule.Resolve(when);
            for (int i = 0; i < Buckets.Count; i++)
            {
                if (Buckets[i].Start == bucket.Start && Buckets[i].End == bucket.End)
                {
                    return Buckets[i].Counts;
                }
            }
            return Daily;
        }

        public string ToJson()
        {
            var sb = new StringBuilder(768);
            sb.Append("{\n");
            sb.Append("  \"date\": \"").Append(Escape(Date)).Append("\",\n");
            sb.Append("  \"dayType\": \"").Append(Escape(DayType)).Append("\",\n");
            sb.Append("  \"daily\": ").Append(CountsJson(Daily)).Append(",\n");
            sb.Append("  \"buckets\": [\n");
            for (int i = 0; i < Buckets.Count; i++)
            {
                var b = Buckets[i];
                sb.Append("    { \"start\": \"").Append(Escape(b.Start))
                  .Append("\", \"end\": \"").Append(Escape(b.End))
                  .Append("\", \"left\": ").Append(b.Counts.Left)
                  .Append(", \"right\": ").Append(b.Counts.Right)
                  .Append(", \"middle\": ").Append(b.Counts.Middle)
                  .Append(", \"total\": ").Append(b.Counts.Total)
                  .Append(", \"scrolls\": ").Append(b.Counts.Scrolls)
                  .Append(", \"keys\": ").Append(b.Counts.Keys)
                  .Append(", \"spaces\": ").Append(b.Counts.Spaces)
                  .Append(", \"enters\": ").Append(b.Counts.Enters)
                  .Append(", \"backspaces\": ").Append(b.Counts.Backspaces)
                  .Append(" }");
                if (i < Buckets.Count - 1) sb.Append(",");
                sb.Append("\n");
            }
            sb.Append("  ]\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        private static string CountsJson(ClickCounts c)
        {
            return "{ \"left\": " + c.Left
                + ", \"right\": " + c.Right
                + ", \"middle\": " + c.Middle
                + ", \"total\": " + c.Total
                + ", \"scrolls\": " + c.Scrolls
                + ", \"keys\": " + c.Keys
                + ", \"spaces\": " + c.Spaces
                + ", \"enters\": " + c.Enters
                + ", \"backspaces\": " + c.Backspaces + " }";
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        public static DayRecord FromJsonOrNull(string json, DateTime fallbackDate)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                var date = ExtractString(json, "date");
                if (string.IsNullOrEmpty(date))
                {
                    date = fallbackDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }

                DateTime parsed;
                if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out parsed))
                {
                    parsed = fallbackDate.Date;
                }

                var record = CreateEmpty(parsed);
                record.DayType = ExtractString(json, "dayType");
                if (string.IsNullOrEmpty(record.DayType))
                {
                    record.DayType = BucketSchedule.DayType(parsed);
                }

                var dailyBlock = ExtractObject(json, "daily");
                if (dailyBlock != null)
                {
                    ApplyCounts(record.Daily, dailyBlock);
                }

                int idx = 0;
                while (true)
                {
                    int startPos = json.IndexOf("\"start\"", idx, StringComparison.Ordinal);
                    if (startPos < 0) break;

                    int objStart = json.LastIndexOf('{', startPos);
                    int objEnd = json.IndexOf('}', startPos);
                    if (objStart < 0 || objEnd < 0) break;

                    var obj = json.Substring(objStart, objEnd - objStart + 1);
                    var start = ExtractString(obj, "start");
                    var end = ExtractString(obj, "end");
                    for (int i = 0; i < record.Buckets.Count; i++)
                    {
                        if (record.Buckets[i].Start == start && record.Buckets[i].End == end)
                        {
                            ApplyCounts(record.Buckets[i].Counts, obj);
                            break;
                        }
                    }
                    idx = objEnd + 1;
                }

                if (record.Daily.Total == 0 && record.Daily.Keys == 0 && record.Daily.Scrolls == 0)
                {
                    for (int i = 0; i < record.Buckets.Count; i++)
                    {
                        record.Daily.AddFrom(record.Buckets[i].Counts);
                    }
                }

                return record;
            }
            catch
            {
                return null;
            }
        }

        private static void ApplyCounts(ClickCounts c, string json)
        {
            c.Left = ExtractLong(json, "left");
            c.Right = ExtractLong(json, "right");
            c.Middle = ExtractLong(json, "middle");
            c.Scrolls = ExtractLong(json, "scrolls");
            c.Keys = ExtractLong(json, "keys");
            c.Spaces = ExtractLong(json, "spaces");
            c.Enters = ExtractLong(json, "enters");
            c.Backspaces = ExtractLong(json, "backspaces");
        }

        private static string ExtractString(string json, string key)
        {
            var pattern = "\"" + key + "\"";
            int i = json.IndexOf(pattern, StringComparison.Ordinal);
            if (i < 0) return null;
            i = json.IndexOf(':', i + pattern.Length);
            if (i < 0) return null;
            i = json.IndexOf('"', i + 1);
            if (i < 0) return null;
            int j = json.IndexOf('"', i + 1);
            if (j < 0) return null;
            return json.Substring(i + 1, j - i - 1);
        }

        private static string ExtractObject(string json, string key)
        {
            var pattern = "\"" + key + "\"";
            int i = json.IndexOf(pattern, StringComparison.Ordinal);
            if (i < 0) return null;
            i = json.IndexOf('{', i + pattern.Length);
            if (i < 0) return null;
            int depth = 0;
            for (int j = i; j < json.Length; j++)
            {
                if (json[j] == '{') depth++;
                else if (json[j] == '}')
                {
                    depth--;
                    if (depth == 0) return json.Substring(i, j - i + 1);
                }
            }
            return null;
        }

        private static long ExtractLong(string json, string key)
        {
            var pattern = "\"" + key + "\"";
            int i = json.IndexOf(pattern, StringComparison.Ordinal);
            if (i < 0) return 0;
            i = json.IndexOf(':', i + pattern.Length);
            if (i < 0) return 0;
            i++;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            int j = i;
            while (j < json.Length && (char.IsDigit(json[j]))) j++;
            if (j == i) return 0;
            long value;
            if (long.TryParse(json.Substring(i, j - i), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }
            return 0;
        }
    }
}
