using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.Models;

public class TimetableModule
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

    // No FK constraint since users is handled by supabase auth, and apparently this is standard practice for supabase auth since they have thier own table
    [JsonIgnore]
    public Guid UserId { get; set; }
    public required string Name { get; set; }

    // The following fields are placed here, instead of TimetableModule since all the modules will havae the same semester and academic year
    public required int Semester { get; set; }
    public required string AcademicYear { get; set; }
    public required List<TimetableModule> MetaData { get; set; }
    public DateTime CreatedAt { get; set; }
}
