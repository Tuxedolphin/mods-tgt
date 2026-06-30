using Backend.Data;
using Backend.DTOs;
using Backend.Exceptions;
using Backend.Models;
using Backend.Services.Rooms;
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
        var userId = await SeedProfileAsync();
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
        var userId = await SeedProfileAsync();
        var timetable = await _service.CreateTimetableAsync(CreateRequest(), userId);

        _context.ChangeTracker.Clear();

        var saved = await _context.Rooms.FindAsync(timetable.Id);

        _context.ChangeTracker.Clear();
        saved.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateTimetableAsync_NewTimetable_ReturnsCorrectResponse()
    {
        var userId = await SeedProfileAsync();
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
    public async Task DeleteTimetableAsync_ExistingTimetable_RemovesRoomRowWithId()
    {
        var userId = await SeedProfileAsync();
        var metaData = CreateMetaData();
        var timetable = await SeedTimetableAsync(userId, metaData);

        await _service.DeleteTimetableAsync(timetable.Id, userId);

        _context.ChangeTracker.Clear();

        var result = await _context.Timetables.FindAsync(timetable.Id);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteTimetableAsync_ExistingTimetable_DoesNotRemoveCorrespondingRoomWithFk()
    {
        var roomId = Guid.NewGuid();

        _context.Rooms.Add(new Room { Id = roomId });

        var userId = await SeedProfileAsync();
        var metaData = CreateMetaData();
        var timetable = await SeedTimetableAsync(userId, metaData);

        await _service.DeleteTimetableAsync(timetable.Id, userId);

        _context.ChangeTracker.Clear();

        var result = await _context.Rooms.FindAsync(roomId);
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteTimetableAsync_ExistingMainTimetable_RemovesRoomWithSameId()
    {
        var roomId = Guid.NewGuid();

        var userId = await SeedProfileAsync();
        var metaData = CreateMetaData();
        var timetable = await SeedTimetableAsync(userId, metaData);

        await _service.DeleteTimetableAsync(timetable.Id, userId);

        _context.ChangeTracker.Clear();

        var result = await _context.Rooms.FindAsync(roomId);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteTimetableAsync_NonExistingTimetable_ThrowsNotFoundException()
    {
        var roomId = Guid.NewGuid();

        var userId = await SeedProfileAsync();
        var metaData = CreateMetaData();
        await SeedTimetableAsync(userId, metaData);

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.DeleteTimetableAsync(Guid.NewGuid(), userId)
        );
    }

    [Fact]
    public async Task DeleteTimetableAsync_WrongUser_ThrowsNotFoundException()
    {
        var roomId = Guid.NewGuid();

        var userId = await SeedProfileAsync();
        var metaData = CreateMetaData();
        var timetable = await SeedTimetableAsync(userId, metaData);

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.DeleteTimetableAsync(timetable.Id, Guid.NewGuid())
        );
    }

    // === FlushDeleteTimetableAsync ===

    [Fact]
    public async Task FlushDeleteTimetableAsync_ExistingTimetable_DeletesRow()
    {
        var userId = await SeedProfileAsync();
        var metaData = CreateMetaData();

        var timetable = await SeedTimetableAsync(userId, metaData, Guid.NewGuid(), Guid.NewGuid());

        await _service.FlushDeleteTimetableAsync(timetable.Id);

        _context.ChangeTracker.Clear();

        var result = await _context.Timetables.FindAsync(timetable.Id);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task FlushDeleteTimetableAsync_NonExistingTimetable_DoesNothing()
    {
        var userId = await SeedProfileAsync();
        var metaData = CreateMetaData();
        var existingTimetable = await SeedTimetableAsync(userId, metaData, Guid.NewGuid());

        var nonExistentId = Guid.NewGuid();

        await Should.NotThrowAsync(() => _service.FlushDeleteTimetableAsync(nonExistentId));

        _context.ChangeTracker.Clear();

        var stillExists = await _context.Timetables.FindAsync(existingTimetable.Id);
        stillExists.ShouldNotBeNull();
    }

    [Fact]
    public async Task FlushDeleteTimetableAsync_DeleteMainTimetable_ThrowsInvalidOperationException()
    {
        var userId = await SeedProfileAsync();
        var metaData = CreateMetaData();

        var timetable = await SeedTimetableAsync(userId, metaData, Guid.NewGuid());

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _service.FlushDeleteTimetableAsync(timetable.Id)
        );
    }

    // === GetTimetableByIdAsync ===

    [Fact]
    public async Task GetTimetableByIdAsync_GetExistingTimetable_ReturnsCorrectResponse()
    {
        var userId = await SeedProfileAsync();
        var metaData = CreateMetaData();
        var timetable = await SeedTimetableAsync(userId, metaData);

        var response = await _service.GetTimetableByIdAsync(timetable.Id, userId);

        response.Id.ShouldBe(timetable.Id);
        response.Name.ShouldBe(timetable.Name);
        response.MetaData.ShouldBe(metaData);
    }

    [Fact]
    public async Task GetTimetableByIdAsync_GetNonExistingTimetable_ThrowsNotFoundException()
    {
        var userId = await SeedProfileAsync();
        var metaData = CreateMetaData();
        var timetable = await SeedTimetableAsync(userId, metaData);

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.GetTimetableByIdAsync(Guid.NewGuid(), Guid.NewGuid())
        );
    }

    [Fact]
    public async Task GetTimetableByIdAsync_GetDifferentUserTimetable_ThrowsNotFoundException()
    {
        var userId = await SeedProfileAsync();
        var metaData = CreateMetaData();
        var timetable = await SeedTimetableAsync(userId, metaData);

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.GetTimetableByIdAsync(timetable.Id, Guid.NewGuid())
        );
    }

    // === GetTimetablesAsync ===

    [Fact]
    public async Task GetTimetablesAsync_WithMainTimetables_ReturnsOnlyUserMainTimetables()
    {
        var userId = await SeedProfileAsync();
        var timetable = await SeedTimetableAsync(userId);

        var result = await _service.GetTimetablesAsync(userId);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(timetable.Id);
        result[0].Name.ShouldBe(timetable.Name);
    }

    [Fact]
    public async Task GetTimetablesAsync_NoTimetables_ReturnsEmptyList()
    {
        var userId = await SeedProfileAsync();

        var result = await _service.GetTimetablesAsync(userId);
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTimetablesAsync_WithOtherUserTimetables_DoesNotReturnThem()
    {
        var userId = await SeedProfileAsync();
        var otherUserId = await SeedProfileAsync();

        await SeedTimetableAsync(otherUserId);

        var result = await _service.GetTimetablesAsync(userId);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTimetablesAsync_WithNonMainTimetable_DoesNotReturnIt()
    {
        var userId = await SeedProfileAsync();
        var mainTimetable = await SeedTimetableAsync(userId);

        var nonMainId = Guid.NewGuid();
        _context.Timetables.Add(
            new Timetable
            {
                Id = nonMainId,
                Name = "Non-main",
                RoomId = mainTimetable.RoomId,
                UserId = userId,
                Semester = 1,
                AcademicYear = "2024-2025",
                MetaData = [],
            }
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
        var userId = await SeedProfileAsync();
        var timetable = await SeedTimetableAsync(userId);
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
        var userId = await SeedProfileAsync();
        var request = new UpdateTimetableRequest { Name = "X", MetaData = [] };

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.UpdateTimetableAsync(Guid.NewGuid(), request, userId)
        );
    }

    [Fact]
    public async Task UpdateTimetableAsync_WrongUser_ThrowsNotFoundException()
    {
        var userId = await SeedProfileAsync();
        var timetable = await SeedTimetableAsync(userId);
        var request = new UpdateTimetableRequest { Name = "X", MetaData = [] };

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.UpdateTimetableAsync(timetable.Id, request, Guid.NewGuid())
        );
    }

    // === UpsertTimetableAsync ===

    [Fact]
    public async Task UpsertTimetableAsync_NewTimetable_InsertsRow()
    {
        var userId = await SeedProfileAsync();
        var mainTimetable = await SeedTimetableAsync(userId);

        var newId = Guid.NewGuid();
        var roomTimetable = new RoomTimetable
        {
            Id = newId,
            UserId = userId,
            Name = "Upserted",
            Semester = 2,
            AcademicYear = "2024-2025",
            MetaData = CreateMetaData(),
            RoomId = mainTimetable.RoomId,
        };

        await _service.UpsertTimetableAsync(roomTimetable);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var saved = await _context.Timetables.FindAsync(newId);
        saved.ShouldNotBeNull();
        saved.Name.ShouldBe("Upserted");
    }

    [Fact]
    public async Task UpsertTimetableAsync_ExistingTimetable_UpdatesFields()
    {
        var userId = await SeedProfileAsync();
        var timetable = await SeedTimetableAsync(userId);

        var updatedMetaData = CreateMetaData(1, "CS3230", "LEC", "#0000FF");

        var roomTimetable = new RoomTimetable
        {
            Id = timetable.Id,
            UserId = userId,
            Name = "Updated via Upsert",
            Semester = 2,
            AcademicYear = "2025-2026",
            MetaData = updatedMetaData,
            RoomId = timetable.RoomId,
        };

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
        var userId = await SeedProfileAsync();
        var mainTimetable = await SeedTimetableAsync(userId);

        var roomTimetable = new RoomTimetable
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Test",
            Semester = 1,
            AcademicYear = "2024-2025",
            MetaData = [],
            RoomId = mainTimetable.RoomId,
        };

        var result = await _service.UpsertTimetableAsync(roomTimetable);

        result.ShouldBeTrue();
    }

    private async Task<Guid> SeedProfileAsync()
    {
        var userId = Guid.NewGuid();

        _context.Profiles.Add(new Profile { Id = userId });
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();
        return userId;
    }

    private async Task<Timetable> SeedTimetableAsync(
        Guid userId,
        List<TimetableModule>? metaData = null,
        Guid? timetableId = null,
        Guid? roomId = null
    )
    {
        var id = timetableId ?? Guid.NewGuid();
        _context.Rooms.Add(new Room { Id = roomId ?? id });

        var timetable = new Timetable
        {
            Id = id,
            Name = "Test",
            RoomId = roomId ?? id,
            UserId = userId,
            Semester = 1,
            AcademicYear = "2024-2025",
            CreatedAt = DateTime.UtcNow,
            MetaData = metaData ?? [],
        };

        _context.Timetables.Add(timetable);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        return timetable;
    }

    private static CreateTimetableRequest CreateRequest(List<TimetableModule>? metaData = null) =>
        new()
        {
            Name = "Test",
            Semester = 1,
            AcademicYear = "2024-2025",
            MetaData = metaData ?? CreateMetaData(),
        };

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

    private static Timetable CreateTimetable(
        Guid? roomId = null,
        Guid? userId = null,
        List<TimetableModule>? metaData = null,
        string name = "Test"
    )
    {
        return new Timetable
        {
            Id = Guid.NewGuid(),
            Name = name,
            RoomId = roomId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Semester = 1,
            AcademicYear = "2024-2025",
            CreatedAt = DateTime.UtcNow,
            MetaData = metaData ?? [],
        };
    }
}
