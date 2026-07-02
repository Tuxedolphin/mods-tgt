using Backend.Data;
using Backend.Models;

namespace Backend.Tests.Services;

public static class TestSeeder
{
    public static async Task<Guid> SeedProfileAsync(this AppDbContext context, string name = "Test")
    {
        var id = Guid.NewGuid();

        context.Profiles.Add(new Profile { Id = id, Username = name });
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        return id;
    }

    public static async Task<Guid> SeedRoomAsync(this AppDbContext context, Guid? id = null)
    {
        var room = new Room { Id = id ?? Guid.NewGuid() };
        context.Rooms.Add(room);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return room.Id;
    }

    public static async Task<Timetable> SeedTimetableAsync(
        this AppDbContext context,
        Guid userId,
        List<TimetableModule>? metaData = null,
        Guid? timetableId = null,
        Guid? roomId = null
    )
    {
        var id = timetableId ?? Guid.NewGuid();
        var actualRoomId = roomId ?? id;

        if (await context.Rooms.FindAsync(actualRoomId) is null)
            context.Rooms.Add(new Room { Id = actualRoomId });

        var timetable = new Timetable
        {
            Id = id,
            Name = "Test",
            RoomId = actualRoomId,
            UserId = userId,
            Semester = 1,
            AcademicYear = "2024-2025",
            CreatedAt = DateTime.UtcNow,
            MetaData = metaData ?? [],
        };

        context.Timetables.Add(timetable);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        return timetable;
    }
}
