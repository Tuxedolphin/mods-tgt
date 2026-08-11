using Backend.Services.NusMods;

namespace Backend.Services.Optimiser;

public record LessonOption(string ClassNo, IReadOnlyList<NusModsSession> Sessions);

public record LessonInput(
    string ModuleCode,
    string LessonType,
    string CurrentClassNo,
    bool Locked,
    IReadOnlyList<LessonOption> Options
);

public record ParticipantInput(
    Guid UserId,
    Guid TimetableId,
    bool Mutable,
    EffectivePreferences Prefs,
    IReadOnlyList<LessonInput> Lessons
);

public record SolveWarning(
    string Code,
    Guid? UserId,
    string? ModuleCode,
    string? LessonType,
    string? Detail
);
