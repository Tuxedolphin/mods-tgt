using System.Text.Json;
using Backend.Models;

namespace Backend.Services.NusMods;

public static class NusModsParsing
{
    private static readonly IReadOnlySet<int> AllTeachingWeeks = new HashSet<int>(
        Enumerable.Range(1, 13)
    );

    public static int ParseTimeToMinutes(string hhmm) =>
        int.Parse(hhmm[..2]) * 60 + int.Parse(hhmm[2..]);

    public static string ToHhmm(int minutes) => $"{minutes / 60:00}{minutes % 60:00}";

    // Weeks come as either a plain array or a {start, end, weeks?} range object
    public static IReadOnlySet<int> ParseWeeks(JsonElement weeks)
    {
        if (weeks.ValueKind == JsonValueKind.Array)
            return weeks.EnumerateArray().Select(w => w.GetInt32()).ToHashSet();

        if (weeks.TryGetProperty("weeks", out var listed) && listed.ValueKind == JsonValueKind.Array)
            return listed.EnumerateArray().Select(w => w.GetInt32()).ToHashSet();

        return AllTeachingWeeks;
    }

    public static ModuleTimetable ParseModule(string moduleCode, int semester, JsonDocument doc)
    {
        var sessions = new List<NusModsSession>();

        if (doc.RootElement.TryGetProperty("semesterData", out var semesters))
        {
            foreach (var sem in semesters.EnumerateArray())
            {
                if (sem.GetProperty("semester").GetInt32() != semester)
                    continue;

                foreach (var lesson in sem.GetProperty("timetable").EnumerateArray())
                {
                    sessions.Add(
                        new NusModsSession(
                            lesson.GetProperty("classNo").GetString()!,
                            lesson.GetProperty("lessonType").GetString()!,
                            Enum.Parse<WeekDay>(lesson.GetProperty("day").GetString()!),
                            ParseTimeToMinutes(lesson.GetProperty("startTime").GetString()!),
                            ParseTimeToMinutes(lesson.GetProperty("endTime").GetString()!),
                            ParseWeeks(lesson.GetProperty("weeks"))
                        )
                    );
                }
            }
        }

        return new ModuleTimetable(moduleCode, sessions);
    }
}
