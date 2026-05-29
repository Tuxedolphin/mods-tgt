using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Models;

public class TimeTableModule
{
    public required string ModuleCode { get; set; } = string.Empty;
    public required string LessonNo { get; set; } = string.Empty;
    public required string LessonType { get; set; } = string.Empty;
}

public class TimeTable
{
    public required Guid Id { get; set; }

    // No FK constraint since users is handled by supabase auth, and apparently this is standard practice for supabase auth since they have thier own table
    [JsonIgnore]
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty; // Name of the time table, selected by user

    // The following fields are placed here, instead of TimeTableModule since all the modules will havae the same semester and academic year
    public required int Semester { get; set; }
    public required string AcademicYear { get; set; } = string.Empty;
    public required List<TimeTableModule> MetaData { get; set; }
    public DateTime CreatedAt { get; set; }
}
