using System.ComponentModel.DataAnnotations;
using Backend.Models;

namespace Backend.DTOs;


public record TimetableSummaryResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required int Semester { get; init; }
    public required string AcademicYear { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public record TimetableResponse : TimetableSummaryResponse
{
    public required List<TimetableModule> MetaData { get; init; }
}

public record TimetableDetailedResponse : TimetableResponse
{
    public required ProfileResponse Profile { get; init; }
}

public record CreateTimetableRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required, Range(1, 4, ErrorMessage = "Semester must be between 1 and 4")]
    public int Semester { get; init; }

    [Required]
    [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "AcademicYear must be in format YYYY-YYYY")]
    public string AcademicYear { get; init; } = string.Empty;

    [Required]
    public required List<TimetableModule> MetaData { get; init; }
}

public record UpdateTimetableRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public IReadOnlyCollection<TimetableModule> MetaData { get; init; } = [];
}
