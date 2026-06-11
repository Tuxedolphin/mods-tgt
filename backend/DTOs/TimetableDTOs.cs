using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.DTOs;

public class TimetableSummaryResponse
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }
    public required int Semester { get; set; }
    public required string AcademicYear { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class CreateTimetableRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, Range(1, 4, ErrorMessage = "Semester must be between 1 and 4")]
    public int Semester { get; set; }

    [Required]
    [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "AcademicYear must be in format YYYY-YYYY")]
    public string AcademicYear { get; set; } = string.Empty;

    [Required]
    public required List<TimetableModule> MetaData { get; set; }
}

public class UpdateTimetableRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public List<TimetableModule> MetaData { get; set; } = [];
}
