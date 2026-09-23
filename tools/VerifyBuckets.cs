using System;
using MouseClickCounter;

internal static class VerifyBuckets
{
    private static int Main()
    {
        var weekday = new DateTime(2026, 9, 18); // Friday
        var weekend = new DateTime(2026, 9, 19); // Saturday

        var wb = BucketSchedule.GetBucketsFor(weekday);
        var we = BucketSchedule.GetBucketsFor(weekend);

        Console.WriteLine("Weekday buckets: " + wb.Length);
        for (int i = 0; i < wb.Length; i++)
            Console.WriteLine("  " + wb[i].Key);

        Console.WriteLine("Weekend buckets: " + we.Length);
        for (int i = 0; i < we.Length; i++)
            Console.WriteLine("  " + we[i].Key);

        // Spot-check resolution
        Check(weekday.Date.AddHours(8), "00:00-09:30");
        Check(weekday.Date.AddHours(9).AddMinutes(30), "09:30-10:30");
        Check(weekday.Date.AddHours(17).AddMinutes(45), "17:30-18:30");
        Check(weekday.Date.AddHours(20), "18:30-24:00");
        Check(weekend.Date.AddHours(11), "00:00-12:00");
        Check(weekend.Date.AddHours(15), "12:00-24:00");

        if (wb.Length != 11 || we.Length != 2)
        {
            Console.WriteLine("FAIL: unexpected bucket counts");
            return 1;
        }

        Console.WriteLine("OK");
        return 0;
    }

    private static void Check(DateTime when, string expectedKey)
    {
        var b = BucketSchedule.Resolve(when);
        if (b.Key != expectedKey)
        {
            throw new Exception("Expected " + expectedKey + " for " + when + " got " + b.Key);
        }
        Console.WriteLine("Resolve " + when.ToString("HH:mm") + " -> " + b.Key);
    }
}
