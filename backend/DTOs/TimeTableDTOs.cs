using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class TimeTableRequest
{
    [Required]
    public Guid Id { get; set; }
}

public class TimeTableSummaryResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Semester { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
}
