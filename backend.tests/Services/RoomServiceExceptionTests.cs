using Backend.Data;
using Backend.DTOs.Mappings;
using Backend.Exceptions;
using Backend.Services.Rooms;
using Backend.Services.Timetables;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static Backend.Tests.Services.TestData;

namespace Backend.Tests.Services;

[Collection(nameof(ServiceTestCollection))]
public class RoomServiceExceptionTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AppDbContext _context;
    private readonly RoomTracker _roomTracker = new();
    private readonly ITimetableService _timetableService = Substitute.For<ITimetableService>();
    private readonly RoomService _service;

    public RoomServiceExceptionTests(DatabaseFixture db)
    {
        _db = db;
        _context = db.CreateContext();

        _service = new RoomService(
            NullLogger<RoomService>.Instance,
            _roomTracker,
            Substitute.For<IProfileTracker>(),
            _timetableService,
            _context
        );
    }

    public async Task InitializeAsync() => await _db.ResetAsync();

    public async Task DisposeAsync() => await _context.DisposeAsync();

    // === CreateOrJoinRoom ===

    [Fact]
    public async Task CreateOrJoinRoom_NonExistentRoom_ThrowsNotFoundException()
    {
        var userId = await _context.SeedProfileAsync();

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.CreateOrJoinRoom(userId, Guid.NewGuid())
        );
    }

    [Fact]
    public async Task CreateOrJoinRoom_NonExistentUser_ThrowsNotFoundException()
    {
        var roomId = await _context.SeedRoomAsync();

        _roomTracker.AddRoom(roomId);

        await Should.ThrowAsync<NotFoundException>(() =>
            _service.CreateOrJoinRoom(Guid.NewGuid(), roomId)
        );
    }

    // === HandleLeaveRoom ===

    [Fact]
    public async Task HandleLeaveRoom_CommitFails_LastUserLeaves_RoomStaysOpenWithStagedChanges()
    {
        _timetableService
            .UpsertTimetableAsync(Arg.Any<RoomTimetable>())
            .Returns(Task.FromException<bool>(new DbUpdateException("simulated")));

        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId).ToRoomTimetable();

        _roomTracker.SetRoom(roomId, [], [timetable]);
        _roomTracker.AddUserToRoom(userId, roomId);
        _roomTracker.AddOrUpdateTimetable(timetable);

        await _service.HandleLeaveRoom(userId, roomId);

        _roomTracker.RoomExists(roomId).ShouldBeTrue();
        _roomTracker.GetChangedTimetables(roomId, out var changed).ShouldBeTrue();
        changed.ShouldContain(t => t.Id == timetable.Id);
    }

    // === CommitChangesAsync ===

    [Fact]
    public async Task CommitChangesAsync_UpsertThrowsDbUpdateException_ReturnsFalse()
    {
        _timetableService
            .UpsertTimetableAsync(Arg.Any<RoomTimetable>())
            .Returns(Task.FromException<bool>(new DbUpdateException("simulated")));

        var roomId = Guid.NewGuid();
        _roomTracker.AddRoom(roomId);

        var timetable = MakeTimetable(roomId).ToRoomTimetable();
        _roomTracker.AddOrUpdateTimetable(timetable);

        var result = await _service.CommitChangesAsync(roomId);

        result.ShouldBeFalse();
        _roomTracker.GetChangedTimetables(roomId, out var changed).ShouldBeTrue();
        changed.ShouldContain(t => t.Id == timetable.Id);
    }

    [Fact]
    public async Task CommitChangesAsync_UpsertThrowsInvalidOperationException_ReturnsFalse()
    {
        _timetableService
            .UpsertTimetableAsync(Arg.Any<RoomTimetable>())
            .Returns(Task.FromException<bool>(new InvalidOperationException("simulated")));

        var roomId = Guid.NewGuid();
        _roomTracker.AddRoom(roomId);

        var timetable = MakeTimetable(roomId).ToRoomTimetable();
        _roomTracker.AddOrUpdateTimetable(timetable);

        var result = await _service.CommitChangesAsync(roomId);

        result.ShouldBeFalse();
        _roomTracker.GetChangedTimetables(roomId, out var changed).ShouldBeTrue();
        changed.ShouldContain(t => t.Id == timetable.Id);
    }

    [Fact]
    public async Task CommitChangesAsync_FlushDeleteThrowsDbUpdateException_KeepsDeletedTimetables()
    {
        _timetableService
            .FlushDeleteTimetableAsync(Arg.Any<Guid>())
            .Returns(Task.FromException(new DbUpdateException("simulated")));

        var roomId = Guid.NewGuid();
        _roomTracker.AddRoom(roomId);

        var timetable = MakeTimetable(roomId).ToRoomTimetable();
        _roomTracker.AddOrUpdateTimetable(timetable);
        _roomTracker.DeleteTimetable(roomId, timetable.Id);

        var result = await _service.CommitChangesAsync(roomId);

        result.ShouldBeFalse();
        _roomTracker.GetDeletedTimetables(roomId).ShouldContain(timetable.Id);
    }
}
