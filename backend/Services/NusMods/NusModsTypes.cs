namespace Backend.Services.NusMods;

public enum WeekDay
{
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday,
}

public record NusModsSession(
    string ClassNo,
    string LessonType,
    WeekDay Day,
    int StartMin,
    int EndMin,
    IReadOnlySet<int> Weeks
);

public record ModuleTimetable(string ModuleCode, IReadOnlyList<NusModsSession> Sessions);
