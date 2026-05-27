using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class TimeTableRequest
{
    [Required]
    public Guid Id { get; set; }
}

public class TimeTableSummaryResponse
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }
    public required int Semester { get; set; }
    public required string AcademicYear { get; set; }
}
