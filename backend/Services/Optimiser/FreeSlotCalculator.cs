using Backend.DTOs;
using Backend.Models;
using Backend.Services.NusMods;

namespace Backend.Services.Optimiser;

public static class FreeSlotCalculator
{
    private const int DayStart = 480;
    private const int DayEnd = 1200;
    private const int MinWindow = 30;

    private static readonly WeekDay[] Weekdays =
    [
        WeekDay.Monday,
        WeekDay.Tuesday,
        WeekDay.Wednesday,
        WeekDay.Thursday,
        WeekDay.Friday,
    ];

    public static IReadOnlyList<FreeSlot> Compute(
        IEnumerable<IReadOnlyList<NusModsSession>> occupiedSessionsPerParticipant
    )
    {
        var all = occupiedSessionsPerParticipant.SelectMany(s => s).ToList();
        var result = new List<FreeSlot>();

        foreach (var day in Weekdays)
        {
            var busy = all.Where(s => s.Day == day)
                .Select(s => (Start: Math.Max(s.StartMin, DayStart), End: Math.Min(s.EndMin, DayEnd)))
                .Where(iv => iv.Start < iv.End)
                .OrderBy(iv => iv.Start)
                .ToList();

            var cursor = DayStart;
            foreach (var (start, end) in busy)
            {
                if (start - cursor >= MinWindow)
                    result.Add(new FreeSlot(day, NusModsParsing.ToHhmm(cursor), NusModsParsing.ToHhmm(start)));
                cursor = Math.Max(cursor, end);
            }

            if (DayEnd - cursor >= MinWindow)
                result.Add(new FreeSlot(day, NusModsParsing.ToHhmm(cursor), NusModsParsing.ToHhmm(DayEnd)));
        }

        return result;
    }
}
