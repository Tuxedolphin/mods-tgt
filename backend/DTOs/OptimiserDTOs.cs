using Backend.Models;

namespace Backend.DTOs;

public record FreeSlot(WeekDay Day, string Start, string End);

public record SolveParticipantRequest
{
    public required Guid UserId { get; init; }
    public Guid? TimetableId { get; init; }
}

public record SolveRequest
{
    public List<SolveParticipantRequest>? Participants { get; init; }
    public List<LockedLesson>? LockedLessons { get; init; }
    public bool IncludeFreeSlots { get; init; }
}

public record SoloSolveRequest
{
    public List<LockedLesson>? LockedLessons { get; init; }
    public PreferencePayload? Preferences { get; init; }
    public bool IncludeFreeSlots { get; init; }
}

public record SaveSuggestionRequest
{
    public required Guid SolveId { get; init; }
    public required int Rank { get; init; }
}

public record LessonChangeResponse
{
    public required string ModuleCode { get; init; }
    public required string LessonType { get; init; }
    public required string From { get; init; }
    public required string To { get; init; }
}

public record SuggestionResponse
{
    public required Guid UserId { get; init; }
    public required Guid TimetableId { get; init; }
    public required List<LessonChangeResponse> Changes { get; init; }
}

public record SharedClassResponse
{
    public required string ModuleCode { get; init; }
    public required string LessonType { get; init; }
    public required string ClassNo { get; init; }
    public required List<Guid> UserIds { get; init; }
}

public record PreferenceOutcomeResponse
{
    public required string Pref { get; init; }
    public WeekDay? Day { get; init; }
    public string? Detail { get; init; }
}

public record UserScoreResponse
{
    public required Guid UserId { get; init; }
    public required int SatisfiedCount { get; init; }
    public required int ViolatedCount { get; init; }
    public List<PreferenceOutcomeResponse>? Satisfied { get; init; }
    public List<PreferenceOutcomeResponse>? Violated { get; init; }
}

public record ScoreResponse
{
    public required int SharedClassCount { get; init; }
    public required List<SharedClassResponse> SharedClasses { get; init; }
    public required List<UserScoreResponse> PerUser { get; init; }
}

public record SolutionResponse
{
    public required int Rank { get; init; }
    public required ScoreResponse Score { get; init; }
    public required List<SuggestionResponse> Suggestions { get; init; }
    public List<FreeSlot>? FreeSlots { get; init; }
}

public record WarningResponse
{
    public required string Code { get; init; }
    public Guid? UserId { get; init; }
    public string? ModuleCode { get; init; }
    public string? LessonType { get; init; }
    public string? Detail { get; init; }
}

public record InfeasibleReasonResponse
{
    public required string Reason { get; init; }
    public Guid? UserId { get; init; }
    public WeekDay? Day { get; init; }
    public string? ModuleCode { get; init; }
    public string? LessonType { get; init; }
}

public record FrozenParticipantResponse
{
    public required Guid UserId { get; init; }
    public required int LessonCount { get; init; }
}

public record SolveResponse
{
    public required string Status { get; init; }
    public required Guid SolveId { get; init; }
    public required List<SolutionResponse> Solutions { get; init; }
    public required List<WarningResponse> Warnings { get; init; }
    public required List<Guid> UsedDefaults { get; init; }
    public required List<FrozenParticipantResponse> Frozen { get; init; }
    public List<InfeasibleReasonResponse>? InfeasibleReasons { get; init; }
}
