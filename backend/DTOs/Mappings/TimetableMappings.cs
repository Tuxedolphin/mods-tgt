using Backend.Models;
using Backend.DTOs;
using Backend.Services.Rooms;

namespace Backend.DTOs.Mappings;

public static class TimetableMappings
{
    public static TimetableResponse ToResponse(this Timetable timetable) =>
        new()
        {
            Id = timetable.Id,
            Name = timetable.Name,
            Semester = timetable.Semester,
            AcademicYear = timetable.AcademicYear,
            CreatedAt = timetable.CreatedAt,
            MetaData = [.. timetable.MetaData],
        };

    public static TimetableSummaryResponse ToSummaryResponse(this Timetable timetable) =>
        new()
        {
            Id = timetable.Id,
            Name = timetable.Name,
            Semester = timetable.Semester,
            AcademicYear = timetable.AcademicYear,
            CreatedAt = timetable.CreatedAt,
        };

    public static TimetableDetailedResponse ToDetailedResponse(
        this RoomTimetable timetable,
        Profile profile
    ) =>
        new()
        {
            Id = timetable.Id,
            Name = timetable.Name,
            Profile = profile.ToResponse(),
            Semester = timetable.Semester,
            AcademicYear = timetable.AcademicYear,
            CreatedAt = timetable.CreatedAt,
            MetaData = [.. timetable.MetaData],
        };

    public static RoomTimetable ToRoomTimetable(this Timetable timetable) =>
        new()
        {
            Id = timetable.Id,
            UserId = timetable.UserId,
            Name = timetable.Name,
            Semester = timetable.Semester,
            AcademicYear = timetable.AcademicYear,
            MetaData = [.. timetable.MetaData],
            RoomId = timetable.RoomId,
            OriginalTimetableId = timetable.OriginalTimetableId,
            CreatedAt = timetable.CreatedAt,
        };

    public static RoomTimetable ApplyUpdate(
        this RoomTimetable timetable,
        UpdateTimetableRequest request
    )
    {
        timetable.Name = request.Name;
        timetable.MetaData = [.. request.MetaData];

        return timetable;
    }

    public static Timetable ToTimetable(this RoomTimetable timetable) =>
        new()
        {
            Id = timetable.Id,
            UserId = timetable.UserId,
            Name = timetable.Name,
            Semester = timetable.Semester,
            AcademicYear = timetable.AcademicYear,
            MetaData = [.. timetable.MetaData],
            RoomId = timetable.RoomId,
            OriginalTimetableId = timetable.OriginalTimetableId,
        };

    public static void ApplyTo(this RoomTimetable timetable, Timetable existing)
    {
        existing.Name = timetable.Name;
        existing.Semester = timetable.Semester;
        existing.AcademicYear = timetable.AcademicYear;
        existing.MetaData = [.. timetable.MetaData];
        existing.OriginalTimetableId = timetable.OriginalTimetableId;
    }
}
