using Backend.Models;

namespace Backend.Services.NusMods;

public record NusModsSession(
    string ClassNo,
    string LessonType,
    WeekDay Day,
    int StartMin,
    int EndMin,
    IReadOnlySet<int> Weeks
);

public record ModuleTimetable(string ModuleCode, IReadOnlyList<NusModsSession> Sessions);
