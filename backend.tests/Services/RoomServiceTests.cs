using Backend.Data;
using Backend.DTOs;
using Backend.DTOs.Mappings;
using Backend.Models;
using Backend.Services.Rooms;
using Backend.Services.Timetables;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static Backend.Tests.Services.TestData;

namespace Backend.Tests.Services;

[Collection(nameof(ServiceTestCollection))]
public class RoomServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AppDbContext _context;
    private readonly RoomService _service;

    private readonly RoomTracker _roomTracker;
    private readonly ProfileTracker _profileTracker;

    public RoomServiceTests(DatabaseFixture db)
    {
        _db = db;
        _context = db.CreateContext();

        _roomTracker = new RoomTracker();
        _profileTracker = new ProfileTracker();

        _service = new RoomService(
            NullLogger<RoomService>.Instance,
            _roomTracker,
            _profileTracker,
            new TimetableService(_context),
            _context
        );
    }

    public async Task DisposeAsync() => await _context.DisposeAsync();

    public async Task InitializeAsync() => await _db.ResetAsync();

    // === Tests ===

    // === RoomExists ===

    [Fact]
    public void RoomExists_ExistingRoom_ReturnsTrue()
    {
        var roomId = Guid.NewGuid();

        _roomTracker.AddRoom(roomId);
        _service.RoomExists(roomId).ShouldBeTrue();
    }

    [Fact]
    public void RoomExists_EmptyRoomTracker_ReturnsFalse()
    {
        _service.RoomExists(Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    public void RoomExists_NonExistingRoom_ReturnsFalse()
    {
        var roomId = Guid.NewGuid();

        _roomTracker.AddRoom(roomId);
        _service.RoomExists(Guid.NewGuid()).ShouldBeFalse();
    }

    // === CreateOrJoinRoom ===

    [Fact]
    public async Task CreateOrJoinRoom_PutsRoomInRoomTracker()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = await _context.SeedProfileAsync();

        await _service.CreateOrJoinRoom(userId, roomId);

        _roomTracker.GetRoomOfUser(userId, out var resId).ShouldBeTrue();
        resId.ShouldBe(roomId);
    }

    [Fact]
    public async Task CreateOrJoinRoom_PutsProfileInRoomTracker()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = await _context.SeedProfileAsync();

        await _service.CreateOrJoinRoom(userId, roomId);

        _profileTracker.GetUserById(userId, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task CreateOrJoinRoom_RemovesUserFromCurrentRoom()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = await _context.SeedProfileAsync();

        var oldRoomId = Guid.NewGuid();
        _roomTracker.SetRoom(oldRoomId, new RoomInit([], [], [], []));
        _roomTracker.AddUserToRoom(userId, oldRoomId);

        await _service.CreateOrJoinRoom(userId, roomId);

        _roomTracker.GetRoomOfUser(userId, out var resId).ShouldBeTrue();
        resId.ShouldBe(roomId);
        _roomTracker.GetUsersInRoom(oldRoomId, out _).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateOrJoinRoom_AddsUserToCorrectRoom_WhenJoiningRoom()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = await _context.SeedProfileAsync();

        _roomTracker.AddRoom(roomId);

        await _service.CreateOrJoinRoom(userId, roomId);

        _roomTracker.GetRoomOfUser(userId, out var resId).ShouldBeTrue();
        resId.ShouldBe(roomId);
    }

    [Fact]
    public async Task CreateOrJoinRoom_ColdRoom_LoadsExistingTimetablesFromDb()
    {
        var userId = await _context.SeedProfileAsync();
        var roomId = await _context.SeedRoomAsync();
        var seeded = await _context.SeedTimetableAsync(userId, null, null, roomId);

        await _service.CreateOrJoinRoom(userId, roomId);

        _roomTracker.GetTimetablesInRoom(roomId, out var timetables).ShouldBeTrue();
        timetables.Count.ShouldBe(1);
        timetables.Single().ShouldMatch(seeded.ToRoomTimetable());
    }

    // === HandleLeaveRoom ===

    [Fact]
    public async Task HandleLeaveRoom_RemovesUserFromRoomTracker()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = await _context.SeedProfileAsync();
        var otherId = Guid.NewGuid();

        _roomTracker.SetRoom(roomId, new RoomInit([userId, otherId], [], [], []));

        await _service.HandleLeaveRoom(userId, roomId);

        _roomTracker.GetRoomOfUser(userId, out _).ShouldBeFalse();
        _roomTracker.GetUsersInRoom(roomId, out var users);
        users.ShouldNotContain(userId);
        users.ShouldContain(otherId);
    }

    [Fact]
    public async Task HandleLeaveRoom_ClosesEmptyRoom()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = await _context.SeedProfileAsync();

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [], []));

        await _service.HandleLeaveRoom(userId, roomId);

        _roomTracker.GetRoomOfUser(userId, out _).ShouldBeFalse();
        _roomTracker.GetUsersInRoom(roomId, out _).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleLeaveRoom_RemoveNonExistentUser_DoesNothing()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = await _context.SeedProfileAsync();

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [], []));

        await Should.NotThrowAsync(() => _service.HandleLeaveRoom(Guid.NewGuid(), roomId));

        _roomTracker.GetUsersInRoom(roomId, out var users).ShouldBeTrue();
        users.ShouldContain(userId);
    }

    [Fact]
    public async Task HandleLeaveRoom_RemoveNonExistentRoom_DoesNothing()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = Guid.NewGuid();

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [], []));

        await Should.NotThrowAsync(() => _service.HandleLeaveRoom(userId, Guid.NewGuid()));

        _roomTracker.GetUsersInRoom(roomId, out var users).ShouldBeTrue();
        users.ShouldContain(userId);
    }

    // === HandleCreateTimetable ===

    [Fact]
    public void HandleCreateTimetable_NonExistentRoom_ReturnsRoomNotFound()
    {
        var result = _service.HandleCreateTimetable(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreateRequest(),
            null
        );
        result.ShouldBe(CreateTimetableResult.RoomNotFound);
    }

    [Fact]
    public void HandleCreateTimetable_ValidRequest_ReturnsSuccess()
    {
        var roomId = Guid.NewGuid();
        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], []));

        var result = _service.HandleCreateTimetable(roomId, Guid.NewGuid(), CreateRequest(), null);

        result.ShouldBe(CreateTimetableResult.Success);
    }

    [Fact]
    public void HandleCreateTimetable_ValidRequest_AddsTimetableToTracker()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], []));

        _service.HandleCreateTimetable(roomId, userId, CreateRequest("Room Timetable"), null);

        _roomTracker.GetTimetablesInRoom(roomId, out var timetables).ShouldBeTrue();
        timetables.ShouldContain(t => t.Name == "Room Timetable" && t.UserId == userId);
    }

    [Fact]
    public void HandleCreateTimetable_DuplicateCopyOf_ReturnsTimetableIdConflict()
    {
        var roomId = Guid.NewGuid();
        var originalId = Guid.NewGuid();

        var existing = MakeTimetable(roomId: roomId, originalTimetableId: originalId)
            .ToRoomTimetable();
        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], [existing]));

        var result = _service.HandleCreateTimetable(
            roomId,
            Guid.NewGuid(),
            CreateRequest(),
            originalId
        );

        result.ShouldBe(CreateTimetableResult.TImetableIdConflict);
    }

    [Fact]
    public void HandleCreateTimetable_UniqueCopyOf_ReturnsSuccess()
    {
        var roomId = Guid.NewGuid();
        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], []));

        var result = _service.HandleCreateTimetable(
            roomId,
            Guid.NewGuid(),
            CreateRequest(),
            Guid.NewGuid()
        );

        result.ShouldBe(CreateTimetableResult.Success);
    }

    // === GetRoomInformationAsync ===

    [Fact]
    public async Task GetRoomInformationAsync_ReturnsCorrectRoomInformation()
    {
        var roomId = Guid.NewGuid();
        var userId = await _context.SeedProfileAsync();
        var timetable = MakeTimetable(roomId, userId).ToRoomTimetable();

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [], [timetable]));

        var roomInformation = await _service.GetRoomInformationAsync(roomId);

        var profile = await _context.Profiles.FindAsync(userId);

        roomInformation.ShouldNotBeNull();

        roomInformation.RoomId.ShouldBe(roomId);
        roomInformation.Timetables.ShouldMatch([timetable.ToDetailedResponse(profile!)]);
        roomInformation.Users.ShouldBe([profile!.ToRoomMemberResponse(RoomRole.Editor)]);
    }

    [Fact]
    public async Task GetRoomInformationAsync_NoRoomExistsInCache_ReturnsNull()
    {
        var res = await _service.GetRoomInformationAsync(Guid.NewGuid());
        res.ShouldBeNull();
    }

    [Fact]
    public async Task GetRoomInformationAsync_EmptyRoom_ReturnsCorrectInformation()
    {
        var roomId = Guid.NewGuid();

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], []));

        var res = await _service.GetRoomInformationAsync(roomId);

        res.ShouldNotBeNull();
        res.RoomId.ShouldBe(roomId);
        res.Users.ShouldBeEmpty();
        res.Timetables.ShouldBeEmpty();
    }

    // === GetProfilesInRoomAsync ===

    [Fact]
    public async Task GetProfilesInRoomAsync_ReturnsCorrectProfilesFromDb()
    {
        var roomId = Guid.NewGuid();
        var userId = await _context.SeedProfileAsync();

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [], []));

        var res = await _service.GetProfilesInRoomAsync(roomId);

        var profile = await _context.Profiles.FindAsync(userId);

        res.ShouldNotBeNull();
        res.Count.ShouldBe(1);
        res.ShouldContain(profile!.ToRoomMemberResponse(RoomRole.Editor));
    }

    [Fact]
    public async Task GetProfilesInRoomAsync_NoRoomExistsInCache_ReturnsNull()
    {
        var res = await _service.GetProfilesInRoomAsync(Guid.NewGuid());

        res.ShouldBeNull();
    }

    [Fact]
    public async Task GetProfilesInRoomAsync_EmptyRoom_ReturnsCorrectEmptyLists()
    {
        var roomId = Guid.NewGuid();
        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], []));

        var res = await _service.GetProfilesInRoomAsync(roomId);

        res.ShouldNotBeNull();
        res.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetProfilesInRoomAsync_UserInCache_ReturnsProfileWithoutDbEntry()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _profileTracker.SetUser(new Profile { Id = userId, Username = "Cached" });
        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [], []));

        var res = await _service.GetProfilesInRoomAsync(roomId);

        res.ShouldNotBeNull();
        res.ShouldBe([new RoomMemberResponse(userId, "Cached", null, RoomRole.Editor)]);
    }

    [Fact]
    public async Task GetProfilesInRoomAsync_UserWithoutProfile_IsSkipped()
    {
        var roomId = Guid.NewGuid();
        var knownUserId = await _context.SeedProfileAsync();
        var unknownUserId = Guid.NewGuid();

        _roomTracker.SetRoom(roomId, new RoomInit([knownUserId, unknownUserId], [], [], []));

        var res = await _service.GetProfilesInRoomAsync(roomId);

        res.ShouldNotBeNull();
        res.Count.ShouldBe(1);
        res.Single().UserId.ShouldBe(knownUserId);
    }

    // === GetTimetablesDetailedInRoomAsync ===

    [Fact]
    public async Task GetTimetablesDetailedInRoomAsync_NoRoomExistsInCache_ReturnsNull()
    {
        var res = await _service.GetTimetablesDetailedInRoomAsync(Guid.NewGuid());

        res.ShouldBeNull();
    }

    [Fact]
    public async Task GetTimetablesDetailedInRoomAsync_ReturnsTimetablesWithProfiles()
    {
        var roomId = Guid.NewGuid();
        var userId = await _context.SeedProfileAsync();
        var timetable = MakeTimetable(roomId, userId).ToRoomTimetable();

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [], [timetable]));

        var res = await _service.GetTimetablesDetailedInRoomAsync(roomId);

        var profile = await _context.Profiles.FindAsync(userId);

        res.ShouldNotBeNull();
        res.ShouldMatch([timetable.ToDetailedResponse(profile!)]);
    }

    [Fact]
    public async Task GetTimetablesDetailedInRoomAsync_OwnerWithoutProfile_TimetableIsSkipped()
    {
        var roomId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId).ToRoomTimetable();

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], [timetable]));

        var res = await _service.GetTimetablesDetailedInRoomAsync(roomId);

        res.ShouldNotBeNull();
        res.ShouldBeEmpty();
    }

    // === AddProfileAsync ===

    [Fact]
    public async Task AddProfileAsync_ProfileInDatabase_AddsToTrackerAndReturnsTrue()
    {
        var userId = await _context.SeedProfileAsync();

        var res = await _service.AddProfileAsync(userId);

        res.ShouldBeTrue();
        _profileTracker.GetUserById(userId, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task AddProfileAsync_ProfileAlreadyInTracker_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        _profileTracker.SetUser(new Profile { Id = userId, Username = "Cached" });

        var res = await _service.AddProfileAsync(userId);

        res.ShouldBeTrue();
    }

    [Fact]
    public async Task AddProfileAsync_NonExistentProfile_ReturnsFalse()
    {
        var res = await _service.AddProfileAsync(Guid.NewGuid());

        res.ShouldBeFalse();
    }

    // === CloseRoom ===

    [Fact]
    public async Task CloseRoom_ExistingRoom_RemovesRoomAndProfilesFromTrackers()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _profileTracker.SetUser(new Profile { Id = userId, Username = "Test" });
        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [], []));

        var res = await _service.CloseRoom(roomId);

        res.ShouldBeTrue();
        _roomTracker.RoomExists(roomId).ShouldBeFalse();
        _profileTracker.GetUserById(userId, out _).ShouldBeFalse();
    }

    [Fact]
    public async Task CloseRoom_NonExistentRoom_ReturnsFalse()
    {
        var res = await _service.CloseRoom(Guid.NewGuid());

        res.ShouldBeFalse();
    }

    // === HandleDeleteTimetable ===

    [Fact]
    public void HandleDeleteTimetable_MainTimetable_ReturnsFalse()
    {
        var roomId = Guid.NewGuid();

        _service.HandleDeleteTimetable(roomId, roomId).ShouldBeFalse();
    }

    [Fact]
    public void HandleDeleteTimetable_NonMainTimetable_RemovesFromTracker()
    {
        var roomId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId).ToRoomTimetable();

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], [timetable]));

        _service.HandleDeleteTimetable(roomId, timetable.Id).ShouldBeTrue();

        _roomTracker.GetTimetablesInRoom(roomId, out var timetables).ShouldBeTrue();
        timetables.ShouldBeEmpty();
    }

    // === HandleUpdateTimetableAsync ===

    [Fact]
    public async Task HandleUpdateTimetableAsync_TimetableExistsInRoomTracker_UpdatesCorrectly()
    {
        var timetable = MakeTimetable().ToRoomTimetable();

        _roomTracker.SetRoom(timetable.RoomId, new RoomInit([], [], [], [timetable]));

        var update = new UpdateTimetableRequest
        {
            Name = "New Timetable",
            MetaData = CreateMetaData(),
        };

        var res = await _service.HandleUpdateTimetableAsync(timetable.RoomId, timetable.Id, update);

        res.ShouldBeTrue();
        _roomTracker.GetTimetablesInRoom(timetable.RoomId, out var timetables).ShouldBeTrue();

        timetables.Count.ShouldBe(1);
        timetables.ElementAt(0).ShouldMatch(timetable.ApplyUpdate(update));
    }

    [Fact]
    public async Task HandleUpdateTimetableAsync_TimetableExistsInRoomTracker_AddsRoomToTracked()
    {
        var timetable = MakeTimetable().ToRoomTimetable();

        _roomTracker.SetRoom(timetable.RoomId, new RoomInit([], [], [], [timetable]));

        var update = new UpdateTimetableRequest
        {
            Name = "New Timetable",
            MetaData = CreateMetaData(),
        };

        var res = await _service.HandleUpdateTimetableAsync(timetable.RoomId, timetable.Id, update);

        res.ShouldBeTrue();
        _roomTracker.GetChangedTimetables(timetable.RoomId, out var timetables);

        timetables.Count.ShouldBe(1);
        timetables.ElementAt(0).ShouldMatch(timetable.ApplyUpdate(update));
    }

    [Fact]
    public async Task HandleUpdateTimetableAsync_TimetableOnlyInDb_LoadsFromDbAndUpdatesCorrectly()
    {
        var userId = await _context.SeedProfileAsync();
        var roomId = await _context.SeedRoomAsync();
        var timetable = await _context.SeedTimetableAsync(userId, null, null, roomId);

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], []));

        var update = new UpdateTimetableRequest
        {
            Name = "New Timetable",
            MetaData = CreateMetaData(),
        };

        var res = await _service.HandleUpdateTimetableAsync(roomId, timetable.Id, update);

        res.ShouldBeTrue();
        _roomTracker.GetTimetablesInRoom(roomId, out var timetables).ShouldBeTrue();

        timetables.Count.ShouldBe(1);
        timetables.ElementAt(0).ShouldMatch(timetable.ToRoomTimetable().ApplyUpdate(update));
    }

    [Fact]
    public async Task HandleUpdateTimetableAsync_TimetableOnlyInDb_AddsToChangedList()
    {
        var userId = await _context.SeedProfileAsync();
        var roomId = await _context.SeedRoomAsync();
        var timetable = await _context.SeedTimetableAsync(userId, null, null, roomId);

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], []));

        var update = new UpdateTimetableRequest
        {
            Name = "New Timetable",
            MetaData = CreateMetaData(),
        };

        var res = await _service.HandleUpdateTimetableAsync(roomId, timetable.Id, update);

        res.ShouldBeTrue();
        _roomTracker.GetChangedTimetables(roomId, out var timetables).ShouldBeTrue();

        timetables.Count.ShouldBe(1);
        timetables.ElementAt(0).ShouldMatch(timetable.ToRoomTimetable().ApplyUpdate(update));
    }

    [Fact]
    public async Task HandleUpdateTimetableAsync_TimetableDoesNotExist_ReturnsFalse()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [], []));

        var res = await _service.HandleUpdateTimetableAsync(
            roomId,
            Guid.NewGuid(),
            new UpdateTimetableRequest()
        );

        res.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleUpdateTimetableAsync_RoomDoesNotExist_ReturnsFalse()
    {
        var res = await _service.HandleUpdateTimetableAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UpdateTimetableRequest()
        );

        res.ShouldBeFalse();
    }

    // === CommitChangesAsync ===

    [Fact]
    public async Task CommitChangesAsync_ChangedTimetables_CorrectlyAddedToDb()
    {
        var roomId = await _context.SeedRoomAsync();

        var userId = await _context.SeedProfileAsync();
        var timetable = MakeTimetable(roomId, userId).ToRoomTimetable();
        var unchangedTimetable = MakeTimetable(roomId, userId).ToRoomTimetable();

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], [unchangedTimetable]));
        _roomTracker.AddOrUpdateTimetable(timetable);

        var success = await _service.CommitChangesAsync(roomId);

        _context.ChangeTracker.Clear();
        var res = await _context.Timetables.FindAsync(timetable.Id);

        success.ShouldBeTrue();

        res.ShouldNotBeNull();
        res.ToRoomTimetable().ShouldMatch(timetable);

        var foundUnchanged = await _context.Timetables.FindAsync(unchangedTimetable.Id);
        foundUnchanged.ShouldBeNull();
    }

    [Fact]
    public async Task CommitChangesAsync_ChangedTimetables_CorrectlyChangedInDb()
    {
        var roomId = await _context.SeedRoomAsync();

        var timetable = (
            await _context.SeedTimetableAsync(await _context.SeedProfileAsync(), null, null, roomId)
        ).ToRoomTimetable();
        var unchangedTimetable = MakeTimetable(roomId).ToRoomTimetable();

        timetable.Name = "Updated";
        timetable.Semester = 2;

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], [unchangedTimetable]));
        _roomTracker.AddOrUpdateTimetable(timetable);

        var success = await _service.CommitChangesAsync(roomId);

        _context.ChangeTracker.Clear();

        var res = await _context.Timetables.FindAsync(timetable.Id);

        success.ShouldBeTrue();

        res.ShouldNotBeNull();
        res.ToRoomTimetable().ShouldMatch(timetable);

        var foundUnchanged = await _context.Timetables.FindAsync(unchangedTimetable.Id);
        foundUnchanged.ShouldBeNull();
    }

    [Fact]
    public async Task CommitChangesAsync_ChangedTimetables_RemovedFromChangedList()
    {
        var roomId = await _context.SeedRoomAsync();

        var userId = await _context.SeedProfileAsync();
        var timetable = MakeTimetable(roomId, userId).ToRoomTimetable();
        var unchangedTimetable = MakeTimetable(roomId, userId).ToRoomTimetable();

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], [unchangedTimetable]));
        _roomTracker.AddOrUpdateTimetable(timetable);

        var success = await _service.CommitChangesAsync(roomId);
        success.ShouldBeTrue();

        _roomTracker.GetChangedTimetables(roomId, out var timetables).ShouldBeTrue();
        timetables.ShouldBeEmpty();
    }

    [Fact]
    public async Task CommitChangesAsync_DeletedTimetables_CorrectlyRemovedFromDb()
    {
        var userId = await _context.SeedProfileAsync();
        var roomId = await _context.SeedRoomAsync();
        var deleted = await _context.SeedTimetableAsync(userId, null, null, roomId);
        var kept = await _context.SeedTimetableAsync(userId, null, null, roomId);

        _roomTracker.SetRoom(
            roomId,
            new RoomInit([], [], [], [deleted.ToRoomTimetable(), kept.ToRoomTimetable()])
        );
        _roomTracker.DeleteTimetable(roomId, deleted.Id);

        var success = await _service.CommitChangesAsync(roomId);
        success.ShouldBeTrue();

        _context.ChangeTracker.Clear();

        (await _context.Timetables.FindAsync(deleted.Id)).ShouldBeNull();
        (await _context.Timetables.FindAsync(kept.Id)).ShouldNotBeNull();
    }

    [Fact]
    public async Task CommitChangesAsync_DeletedTimetables_RemovedFromChangedList()
    {
        var roomId = await _context.SeedRoomAsync();

        var userId = await _context.SeedProfileAsync();
        var timetable = MakeTimetable(roomId, userId).ToRoomTimetable();
        var unchangedTimetable = MakeTimetable(roomId, userId).ToRoomTimetable();

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [], [unchangedTimetable, timetable]));
        _roomTracker.DeleteTimetable(roomId, timetable.Id);

        var success = await _service.CommitChangesAsync(roomId);
        success.ShouldBeTrue();

        _roomTracker.GetChangedTimetables(roomId, out var timetables).ShouldBeTrue();
        timetables.ShouldBeEmpty();
        _roomTracker.GetDeletedTimetables(roomId).ShouldBeEmpty();
    }

    [Fact]
    public async Task CommitChangesAsync_NothingToCommit_ReturnsTrueAndLeavesDatabaseUnchanged()
    {
        var roomId = Guid.NewGuid();
        _roomTracker.AddRoom(roomId);

        var userId = await _context.SeedProfileAsync();
        var untouched = await _context.SeedTimetableAsync(userId);

        var result = await _service.CommitChangesAsync(roomId);

        result.ShouldBeTrue();

        _context.ChangeTracker.Clear();

        var stillThere = await _context.Timetables.FindAsync(untouched.Id);

        stillThere.ShouldNotBeNull();
        stillThere.Name.ShouldBe(untouched.Name);
    }

    [Fact]
    public async Task CommitChangesAsync_FlushDeleteThrowsInvalidOperation_ReturnsFalse()
    {
        var userId = await _context.SeedProfileAsync();
        var mainTimetable = await _context.SeedTimetableAsync(userId);

        _roomTracker.SetRoom(
            mainTimetable.RoomId,
            new RoomInit([], [], [], [mainTimetable.ToRoomTimetable()])
        );
        _roomTracker.DeleteTimetable(mainTimetable.RoomId, mainTimetable.Id);

        var result = await _service.CommitChangesAsync(mainTimetable.RoomId);

        result.ShouldBeFalse();
    }

    private static CreateTimetableRequest CreateRequest(string name = "Test") =>
        new()
        {
            Name = name,
            Semester = 1,
            AcademicYear = "2024-2025",
            MetaData = [],
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
}
