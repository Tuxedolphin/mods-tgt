using Backend.Data;
using Backend.DTOs;
using Backend.DTOs.Mappings;
using Backend.Exceptions;
using Backend.Models;
using Backend.Services.Timetables;
using Shouldly;

namespace Backend.Tests.Services;

[Collection(nameof(ServiceTestCollection))]
public class TimetableServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AppDbContext _context;
    private readonly TimetableService _service;

    public TimetableServiceTests(DatabaseFixture db)
    {
        _db = db;
        _context = db.CreateContext();
        _service = new TimetableService(_context);
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    public async Task InitializeAsync() => await _db.ResetAsync();

    // === Tests ===

    // === CreateTimetableAsync ===

    [Fact]
    public async Task CreateTimetableAsync_NewTimetable_CreatesTimetableRow()
    {
        var userId = await _context.SeedProfileAsync();
        var metaData = CreateMetaData();
        var request = CreateRequest(metaData);

        var timetable = await _service.CreateTimetableAsync(request, userId);

        _context.ChangeTracker.Clear();
        var saved = await _context.Timetables.FindAsync(timetable.Id);

        _context.ChangeTracker.Clear();

        saved.ShouldNotBeNull();
        saved.Name.ShouldBe(request.Name);
        saved.Semester.ShouldBe(request.Semester);
        saved.AcademicYear.ShouldBe(request.AcademicYear);
        saved.MetaData.ShouldBe(metaData);
    }

    [Fact]
    public async Task CreateTimetableAsync_NewTimetable_CreatesRoomRowWithSameId()
    {
        var userId = await _context.SeedProfileAsync();
        var timetable = await _service.CreateTimetableAsync(CreateRequest(), userId);

        _context.ChangeTracker.Clear();

        var saved = await _context.Rooms.FindAsync(timetable.Id);

        _context.ChangeTracker.Clear();
        saved.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateTimetableAsync_NewTimetable_ReturnsCorrectResponse()
    {
        var userId = await _context.SeedProfileAsync();
        var metaData = CreateMetaData();
        var request = CreateRequest(metaData);

        var timetable = await _service.CreateTimetableAsync(request, userId);

        _context.ChangeTracker.Clear();

        timetable.Name.ShouldBe(request.Name);
        timetable.Semester.ShouldBe(request.Semester);
        timetable.AcademicYear.ShouldBe(request.AcademicYear);
        timetable.MetaData.ShouldBe(metaData);
    }

    // === DeleteTimetableAsync ===

    [Fact]
    public async Task DeleteTimetableAsync_ExistingTimetable_RemovesTimetableRow()
    {
        var userId = await _context.SeedProfileAsync();
        var metaData = CreateMetaData();
        var timetable = await _context.SeedTimetableAsync(userId, metaData);

        await _service.DeleteTimetableAsync(timetable.Id, userId);

        _context.ChangeTracker.Clear();

        var result = await _context.Timetables.FindAsync(timetable.Id);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteTimetableAsync_MainTimetable_AlsoRemovesRoom()
    {
        var userId = await _context.SeedProfileAsync();
        var timetable = await _context.SeedTimetableAsync(userId);

        await _service.DeleteTimetableAsync(timetable.Id, userId);

        _context.ChangeTracker.Clear();
        var room = await _context.Rooms.FindAsync(timetable.RoomId);

        room.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteTimetableAsync_NonMainTimetable_DoesNotRemoveRoom()
    {
        var userId = await _context.SeedProfileAsync();
        var mainTimetable = await _context.SeedTimetableAsync(userId);

        var nonMainId = Guid.NewGuid();
        var nonMainTimetable = await _context.SeedTimetableAsync(
            userId,
            timetableId: nonMainId,
            roomId: mainTimetable.RoomId
        );

        await _service.DeleteTimetableAsync(nonMainTimetable.Id, userId);

        _context.ChangeTracker.Clear();
        var room = await _context.Rooms.FindAsync(mainTimetable.RoomId);

        room.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteTimetableAsync_NonExistingTimetable_ThrowsNotFoundException()
    {
        var userId = await _context.SeedProfileAsync();
        var metaData = CreateMetaData();
        await _context.SeedTimetableAsync(userId, metaData);

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.DeleteTimetableAsync(Guid.NewGuid(), userId)
        );
    }

    [Fact]
    public async Task DeleteTimetableAsync_WrongUser_ThrowsNotFoundException()
    {
        var userId = await _context.SeedProfileAsync();
        var metaData = CreateMetaData();
        var timetable = await _context.SeedTimetableAsync(userId, metaData);

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.DeleteTimetableAsync(timetable.Id, Guid.NewGuid())
        );
    }

    // === FlushDeleteTimetableAsync ===

    [Fact]
    public async Task FlushDeleteTimetableAsync_ExistingTimetable_DeletesRow()
    {
        var userId = await _context.SeedProfileAsync();
        var metaData = CreateMetaData();

        var timetable = await _context.SeedTimetableAsync(
            userId,
            metaData,
            Guid.NewGuid(),
            Guid.NewGuid()
        );

        await _service.FlushDeleteTimetableAsync(timetable.Id);

        _context.ChangeTracker.Clear();

        var result = await _context.Timetables.FindAsync(timetable.Id);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task FlushDeleteTimetableAsync_NonExistingTimetable_DoesNothing()
    {
        var userId = await _context.SeedProfileAsync();
        var metaData = CreateMetaData();
        var existingTimetable = await _context.SeedTimetableAsync(userId, metaData, Guid.NewGuid());

        var nonExistentId = Guid.NewGuid();

        await Should.NotThrowAsync(() => _service.FlushDeleteTimetableAsync(nonExistentId));

        _context.ChangeTracker.Clear();

        var stillExists = await _context.Timetables.FindAsync(existingTimetable.Id);
        stillExists.ShouldNotBeNull();
    }

    [Fact]
    public async Task FlushDeleteTimetableAsync_DeleteMainTimetable_ThrowsInvalidOperationException()
    {
        var userId = await _context.SeedProfileAsync();
        var metaData = CreateMetaData();

        var timetable = await _context.SeedTimetableAsync(userId, metaData, Guid.NewGuid());

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _service.FlushDeleteTimetableAsync(timetable.Id)
        );
    }

    // === GetTimetableByIdAsync ===

    [Fact]
    public async Task GetTimetableByIdAsync_GetExistingTimetable_ReturnsCorrectResponse()
    {
        var userId = await _context.SeedProfileAsync();
        var metaData = CreateMetaData();
        var timetable = await _context.SeedTimetableAsync(userId, metaData);

        var response = await _service.GetTimetableByIdAsync(timetable.Id, userId);

        response.Id.ShouldBe(timetable.Id);
        response.Name.ShouldBe(timetable.Name);
        response.MetaData.ShouldBe(metaData);
    }

    [Fact]
    public async Task GetTimetableByIdAsync_NonExistingTimetable_ThrowsNotFoundException()
    {
        await Should.ThrowAsync<NotFoundException>(() =>
            _service.GetTimetableByIdAsync(Guid.NewGuid(), Guid.NewGuid())
        );
    }

    [Fact]
    public async Task GetTimetableByIdAsync_GetDifferentUserTimetable_ThrowsNotFoundException()
    {
        var userId = await _context.SeedProfileAsync();
        var metaData = CreateMetaData();
        var timetable = await _context.SeedTimetableAsync(userId, metaData);

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.GetTimetableByIdAsync(timetable.Id, Guid.NewGuid())
        );
    }

    // === GetTimetablesAsync ===

    [Fact]
    public async Task GetTimetablesAsync_ReturnsAllUserMainTimetables()
    {
        var userId = await _context.SeedProfileAsync();
        var first = await _context.SeedTimetableAsync(userId);
        var second = await _context.SeedTimetableAsync(userId);

        var result = await _service.GetTimetablesAsync(userId);

        result.Count.ShouldBe(2);
        result.Select(t => t.Id).ShouldContain(first.Id);
        result.Select(t => t.Id).ShouldContain(second.Id);
    }

    [Fact]
    public async Task GetTimetablesAsync_NoTimetables_ReturnsEmptyList()
    {
        var userId = await _context.SeedProfileAsync();

        var result = await _service.GetTimetablesAsync(userId);
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTimetablesAsync_WithOtherUserTimetables_DoesNotReturnThem()
    {
        var userId = await _context.SeedProfileAsync();
        var otherUserId = await _context.SeedProfileAsync();

        await _context.SeedTimetableAsync(otherUserId);

        var result = await _service.GetTimetablesAsync(userId);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTimetablesAsync_WithNonMainTimetable_DoesNotReturnIt()
    {
        var userId = await _context.SeedProfileAsync();
        var mainTimetable = await _context.SeedTimetableAsync(userId);

        _context.Timetables.Add(
            CreateTimetable(roomId: mainTimetable.RoomId, userId: userId, name: "Non-main")
        );
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var result = await _service.GetTimetablesAsync(userId);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(mainTimetable.Id);
    }

    // === UpdateTimetableAsync ===

    [Fact]
    public async Task UpdateTimetableAsync_ExistingTimetable_UpdatesNameAndMetaData()
    {
        var userId = await _context.SeedProfileAsync();
        var timetable = await _context.SeedTimetableAsync(userId);
        var newMetaData = CreateMetaData(2, "CS2030", "TUT", "#00FF00");

        var request = new UpdateTimetableRequest { Name = "Updated", MetaData = newMetaData };
        await _service.UpdateTimetableAsync(timetable.Id, request, userId);

        _context.ChangeTracker.Clear();

        var saved = await _context.Timetables.FindAsync(timetable.Id);

        saved.ShouldNotBeNull();
        saved.Name.ShouldBe("Updated");
        saved.MetaData.ShouldBe(newMetaData);
    }

    [Fact]
    public async Task UpdateTimetableAsync_NonExistingTimetable_ThrowsNotFoundException()
    {
        var userId = await _context.SeedProfileAsync();
        var request = new UpdateTimetableRequest { Name = "X", MetaData = [] };

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.UpdateTimetableAsync(Guid.NewGuid(), request, userId)
        );
    }

    [Fact]
    public async Task UpdateTimetableAsync_WrongUser_ThrowsNotFoundException()
    {
        var userId = await _context.SeedProfileAsync();
        var timetable = await _context.SeedTimetableAsync(userId);
        var request = new UpdateTimetableRequest { Name = "X", MetaData = [] };

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.UpdateTimetableAsync(timetable.Id, request, Guid.NewGuid())
        );
    }

    // === UpsertTimetableAsync ===

    [Fact]
    public async Task UpsertTimetableAsync_NewTimetable_InsertsRow()
    {
        var userId = await _context.SeedProfileAsync();
        var mainTimetable = await _context.SeedTimetableAsync(userId);

        var newTimetable = CreateTimetable(
            roomId: mainTimetable.RoomId,
            userId: userId,
            name: "Upserted"
        );
        var roomTimetable = newTimetable.ToRoomTimetable();

        await _service.UpsertTimetableAsync(roomTimetable);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var saved = await _context.Timetables.FindAsync(newTimetable.Id);
        saved.ShouldNotBeNull();
        saved.Name.ShouldBe("Upserted");
    }

    [Fact]
    public async Task UpsertTimetableAsync_ExistingTimetable_UpdatesFields()
    {
        var userId = await _context.SeedProfileAsync();
        var timetable = await _context.SeedTimetableAsync(userId);

        var updatedMetaData = CreateMetaData(1, "CS3230", "LEC", "#0000FF");

        var roomTimetable = timetable.ToRoomTimetable();
        roomTimetable.Name = "Updated via Upsert";
        roomTimetable.Semester = 2;
        roomTimetable.AcademicYear = "2025-2026";
        roomTimetable.MetaData = updatedMetaData;

        await _service.UpsertTimetableAsync(roomTimetable);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();
        var saved = await _context.Timetables.FindAsync(timetable.Id);

        saved.ShouldNotBeNull();

        saved.Name.ShouldBe("Updated via Upsert");
        saved.Semester.ShouldBe(2);
        saved.AcademicYear.ShouldBe("2025-2026");
        saved.MetaData.ShouldBe(updatedMetaData);
    }

    [Fact]
    public async Task UpsertTimetableAsync_ReturnsTrue()
    {
        var userId = await _context.SeedProfileAsync();
        var mainTimetable = await _context.SeedTimetableAsync(userId);
        var roomTimetable = CreateTimetable(roomId: mainTimetable.RoomId, userId: userId)
            .ToRoomTimetable();

        var result = await _service.UpsertTimetableAsync(roomTimetable);

        result.ShouldBeTrue();
    }

    private static CreateTimetableRequest CreateRequest(List<TimetableModule>? metaData = null) =>
        new()
        {
            Name = "Test",
            Semester = 1,
            AcademicYear = "2024-2025",
            MetaData = metaData ?? CreateMetaData(),
        };

    private static Timetable CreateTimetable(
        Guid? roomId = null,
        Guid? userId = null,
        List<TimetableModule>? metaData = null,
        string name = "Test"
    )
    {
        var id = Guid.NewGuid();
        return new Timetable
        {
            Id = id,
            Name = name,
            RoomId = roomId ?? id,
            UserId = userId ?? Guid.NewGuid(),
            Semester = 1,
            AcademicYear = "2024-2025",
            CreatedAt = DateTime.UtcNow,
            MetaData = metaData ?? [],
        };
    }

    private static List<TimetableModule> CreateMetaData(
        int count = 1,
        string moduleCode = "CS1010",
        string lessonType = "LEC",
        string colour = "#FF0000"
    )
    {
        return
        [
            .. Enumerable
                .Range(1, count)
                .Select(i => new TimetableModule
                {
                    ModuleCode = moduleCode,
                    LessonNo = $"{i:D2}",
                    LessonType = lessonType,
                    Colour = colour,
                }),
        ];
    }
}
