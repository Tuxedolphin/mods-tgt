using Backend.Models;

namespace Backend.Tests.Services;

public static class TestData
{
    public static Timetable MakeTimetable(
        Guid? roomId = null,
        Guid? userId = null,
        Guid? originalTimetableId = null,
        string name = "Test"
    )
    {
        var id = Guid.NewGuid();

        return new()
        {
            Id = id,
            UserId = userId ?? Guid.NewGuid(),
            Name = name,
            Semester = 1,
            AcademicYear = "2024-2025",
            MetaData = [],
            RoomId = roomId ?? id,
            OriginalTimetableId = originalTimetableId,
        };
    }
}
