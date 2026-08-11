namespace Backend.Models;

public enum Tier
{
    Off,
    NiceToHave,
    Important,
}

public record TimeWindow
{
    public required string Start { get; set; }
    public required string End { get; set; }
}

public record LockedLesson
{
    public required string ModuleCode { get; set; }
    public required string LessonType { get; set; }
}

public record PreferencePayload
{
    public List<WeekDay>? BlockedDays { get; set; }
    public string? EarliestStart { get; set; }
    public string? LatestEnd { get; set; }
    public TimeWindow? PreferredWindow { get; set; }
    public Tier? LunchBreak { get; set; }
    public Tier? CompactDays { get; set; }
    public Tier? FewerCampusDays { get; set; }
    public int? MaxConsecutiveHours { get; set; }
    public WeekDay? FreeDay { get; set; }
    public List<LockedLesson>? LockedLessons { get; set; }
}

public class OptimiserPreference
{
    public Guid Id { get; set; }
    public required Guid UserId { get; set; }

    // Null for the user's global row, set for a per-room override
    public Guid? RoomId { get; set; }
    public Room? Room { get; set; }

    public required PreferencePayload Payload { get; set; }
}
