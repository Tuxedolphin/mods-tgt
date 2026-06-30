using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public record TimetableModule
{
    [Required]
    public required string ModuleCode { get; set; } = string.Empty;

    [Required]
    public required string LessonNo { get; set; } = string.Empty;

    [Required]
    public required string LessonType { get; set; } = string.Empty;

    [Required]
    public required string Colour { get; set; } = string.Empty;
}

public class Timetable
{
    public required Guid Id { get; set; }

    // No FK constraint since users is handled by supabase auth,
    // and apparently this is standard practice for supabase auth since they have thier own table
    public required Guid UserId { get; set; }

    public required string Name { get; set; } = null!;

    // The following fields are placed here, instead of TimetableModule since all the modules will havae the same semester and academic year
    public required int Semester { get; set; }
    public required string AcademicYear { get; set; } = null!;

    public required ICollection<TimetableModule> MetaData { get; set; } = null!;

    // We will always have a room attached to each timetable
    public required Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    // FK to itself (the original if this is a copy, else null) and the room
    public Guid? OriginalTimetableId { get; set; }
    public Timetable? OriginalTimetable { get; set; }

    // Basic info
    public DateTime CreatedAt { get; set; }
}
