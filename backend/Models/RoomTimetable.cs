namespace Backend.Models;

public class RoomTimetable
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Name { get; set; }
    public required int Semester { get; set; }
    public required string AcademicYear { get; set; }
    public required ICollection<TimetableModule> MetaData { get; set; }
    public required Guid RoomId { get; set; }
    public Guid? OriginalTimetableId { get; set; }
    public DateTime CreatedAt { get; set; }
}
