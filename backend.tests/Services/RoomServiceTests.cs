using Backend.Data;
using Backend.DTOs;
using Backend.DTOs.Mappings;
using Backend.Exceptions;
using Backend.Models;
using Backend.Services.Profiles;
using Backend.Services.Rooms;
using Backend.Services.Timetables;
using Microsoft.EntityFrameworkCore;
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
    private readonly IAvatarUrlProvider _avatarUrlProvider;
    private readonly IProfileResponseMapper _profileResponseMapper;

    public RoomServiceTests(DatabaseFixture db)
    {
        _db = db;
        _context = db.CreateContext();

        _roomTracker = new RoomTracker();
        _profileTracker = new ProfileTracker();
        _avatarUrlProvider = new TestAvatarUrlProvider();
        _profileResponseMapper = new ProfileResponseMapper(_avatarUrlProvider);

        _service = new RoomService(
            NullLogger<RoomService>.Instance,
            _roomTracker,
            _profileTracker,
            new TimetableService(_context),
            _profileResponseMapper,
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
    public async Task CreateOrJoinRoomAsync_PutsRoomInRoomTracker()
    {
        var userId = await _context.SeedProfileAsync();
        var roomId = Guid.NewGuid();
        const string connectionId = "connection";
        await _context.SeedTimetableAsync(userId, timetableId: roomId, roomId: roomId);
        await _service.RegisterConnectionAsync(userId, connectionId);

        await _service.CreateOrJoinRoomAsync(roomId, userId, connectionId);

        _roomTracker.TryGetRoomOfConnection(connectionId, out var resId).ShouldBeTrue();
        resId.ShouldBe(roomId);
    }

    [Fact]
    public async Task CreateOrJoinRoomAsync_PutsProfileInRoomTracker()
    {
        var userId = await _context.SeedProfileAsync();
        var roomId = Guid.NewGuid();
        const string connectionId = "connection";
        await _context.SeedTimetableAsync(userId, timetableId: roomId, roomId: roomId);
        await _service.RegisterConnectionAsync(userId, connectionId);

        await _service.CreateOrJoinRoomAsync(roomId, userId, connectionId);

        _profileTracker.TryGetUserById(userId, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task CreateOrJoinRoomAsync_UserHasAnotherConnection_PreservesPreviousRoomPresence()
    {
        var userId = await _context.SeedProfileAsync();
        var roomId = Guid.NewGuid();
        const string connectionId = "connection";
        await _context.SeedTimetableAsync(userId, timetableId: roomId, roomId: roomId);
        await _service.RegisterConnectionAsync(userId, connectionId);

        var oldRoomId = Guid.NewGuid();
        _roomTracker.SetRoom(oldRoomId, new RoomInit([], [userId], []));
        _roomTracker.MoveConnectionToRoom(connectionId, userId, oldRoomId);
        _roomTracker.RegisterConnection("other-connection", userId);
        _roomTracker.MoveConnectionToRoom("other-connection", userId, oldRoomId);

        await _service.CreateOrJoinRoomAsync(roomId, userId, connectionId);

        _roomTracker.TryGetRoomOfConnection(connectionId, out var resId).ShouldBeTrue();
        resId.ShouldBe(roomId);
        _roomTracker.TryGetUsersInRoom(oldRoomId, out var users).ShouldBeTrue();
        users.ShouldBe([userId]);
    }

    [Fact]
    public async Task CreateOrJoinRoomAsync_AssociatesConnectionWithCorrectRoom_WhenJoiningRoom()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = await _context.SeedProfileAsync();
        const string connectionId = "connection";

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], []));
        await _service.RegisterConnectionAsync(userId, connectionId);

        await _service.CreateOrJoinRoomAsync(roomId, userId, connectionId);

        _roomTracker.TryGetRoomOfConnection(connectionId, out var resId).ShouldBeTrue();
        resId.ShouldBe(roomId);
    }

    [Fact]
    public async Task CreateOrJoinRoomAsync_ColdRoom_LoadsExistingTimetablesFromDb()
    {
        var userId = await _context.SeedProfileAsync();
        var roomId = Guid.NewGuid();
        const string connectionId = "connection";
        var seeded = await _context.SeedTimetableAsync(userId, timetableId: roomId, roomId: roomId);
        await _service.RegisterConnectionAsync(userId, connectionId);

        await _service.CreateOrJoinRoomAsync(roomId, userId, connectionId);

        _roomTracker.TryGetTimetablesInRoom(roomId, out var timetables).ShouldBeTrue();
        timetables.Count.ShouldBe(1);
        timetables.Single().ShouldMatch(seeded.ToRoomTimetable());
    }

    // === HandleLeaveRoom ===

    [Fact]
    public async Task HandleLeaveRoomAsync_UserHasAnotherConnection_PreservesUserPresence()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = await _context.SeedProfileAsync();
        const string connectionId = "connection";

        _roomTracker.SetRoom(roomId, new RoomInit([], [userId], []));
        _roomTracker.RegisterConnection(connectionId, userId);
        _roomTracker.MoveConnectionToRoom(connectionId, userId, roomId);
        _roomTracker.RegisterConnection("other-connection", userId);
        _roomTracker.MoveConnectionToRoom("other-connection", userId, roomId);

        await _service.HandleLeaveRoomAsync(roomId, connectionId);

        _roomTracker.TryGetRoomOfConnection(connectionId, out _).ShouldBeFalse();
        _roomTracker
            .TryGetRoomOfConnection("other-connection", out var currentRoomId)
            .ShouldBeTrue();
        currentRoomId.ShouldBe(roomId);
        _roomTracker.TryGetUsersInRoom(roomId, out var users).ShouldBeTrue();
        users.ShouldBe([userId]);
    }

    [Fact]
    public async Task HandleLeaveRoomAsync_ClosesEmptyRoom()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = await _context.SeedProfileAsync();
        const string connectionId = "connection";

        _roomTracker.SetRoom(roomId, new RoomInit([], [userId], []));
        _roomTracker.RegisterConnection(connectionId, userId);
        _roomTracker.MoveConnectionToRoom(connectionId, userId, roomId);

        await _service.HandleLeaveRoomAsync(roomId, connectionId);

        _roomTracker.TryGetRoomOfConnection(connectionId, out _).ShouldBeFalse();
        _roomTracker.RoomExists(roomId).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleLeaveRoomAsync_MissingConnection_DoesNothing()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = await _context.SeedProfileAsync();
        const string connectionId = "connection";

        _roomTracker.SetRoom(roomId, new RoomInit([], [userId], []));
        _roomTracker.RegisterConnection(connectionId, userId);
        _roomTracker.MoveConnectionToRoom(connectionId, userId, roomId);

        await Should.NotThrowAsync(() => _service.HandleLeaveRoomAsync(roomId, "missing"));

        _roomTracker.TryGetUsersInRoom(roomId, out var users).ShouldBeTrue();
        users.ShouldContain(userId);
    }

    [Fact]
    public async Task HandleLeaveRoomAsync_WrongRoom_DoesNothing()
    {
        var roomId = await _context.SeedRoomAsync();
        var userId = Guid.NewGuid();
        const string connectionId = "connection";

        _roomTracker.SetRoom(roomId, new RoomInit([], [userId], []));
        _roomTracker.RegisterConnection(connectionId, userId);
        _roomTracker.MoveConnectionToRoom(connectionId, userId, roomId);

        await Should.NotThrowAsync(() =>
            _service.HandleLeaveRoomAsync(Guid.NewGuid(), connectionId)
        );

        _roomTracker.TryGetUsersInRoom(roomId, out var users).ShouldBeTrue();
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
        var userId = Guid.NewGuid();
        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], []));

        var result = _service.HandleCreateTimetable(roomId, userId, CreateRequest(), null);

        result.ShouldBe(CreateTimetableResult.Success);
    }

    [Fact]
    public void HandleCreateTimetable_ValidRequest_AddsTimetableToTracker()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], []));

        _service.HandleCreateTimetable(roomId, userId, CreateRequest("Room Timetable"), null);

        _roomTracker.TryGetTimetablesInRoom(roomId, out var timetables).ShouldBeTrue();
        timetables.ShouldContain(t => t.Name == "Room Timetable" && t.UserId == userId);
    }

    [Fact]
    public void HandleCreateTimetable_DuplicateCopyOf_ReturnsTimetableIdConflict()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var originalId = Guid.NewGuid();

        var existing = MakeTimetable(roomId: roomId, originalTimetableId: originalId)
            .ToRoomTimetable();
        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [existing]));

        var result = _service.HandleCreateTimetable(roomId, userId, CreateRequest(), originalId);

        result.ShouldBe(CreateTimetableResult.TimetableIdConflict);
    }

    [Fact]
    public void HandleCreateTimetable_UniqueCopyOf_ReturnsSuccess()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], []));

        var result = _service.HandleCreateTimetable(
            roomId,
            userId,
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
        var timetableModel = MakeTimetable(roomId, userId);
        timetableModel.Id = roomId;
        var timetable = timetableModel.ToRoomTimetable();

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [timetable]));
        _roomTracker.RegisterConnection("owner-connection", userId);
        _roomTracker.MoveConnectionToRoom("owner-connection", userId, roomId);

        var roomInformation = await _service.GetRoomInformationAsync(roomId, userId);

        var profile = await _context.Profiles.FindAsync(userId);

        roomInformation.ShouldNotBeNull();

        roomInformation.RoomId.ShouldBe(roomId);
        roomInformation.Timetables.ShouldMatch([
            timetable.ToDetailedResponse(_profileResponseMapper.ToResponse(profile!)),
        ]);
        roomInformation.Members.ShouldBe([
            _profileResponseMapper.ToRoomMemberResponse(profile!, RoomRole.Owner, true),
        ]);
    }

    [Fact]
    public async Task GetRoomInformationAsync_NoRoomExistsInCache_ReturnsNull()
    {
        var res = await _service.GetRoomInformationAsync(Guid.NewGuid(), Guid.NewGuid());
        res.ShouldBeNull();
    }

    // === GetRoomMembersAsync ===

    [Fact]
    public async Task GetRoomMembersAsync_ReturnsOwnerAndOfflineMembersWithRolesAndPresence()
    {
        var roomId = Guid.NewGuid();
        var ownerId = await _context.SeedProfileAsync(name: "Owner", handle: "owner");
        var editorId = await _context.SeedProfileAsync(name: "Editor", handle: "editor");
        var viewerId = await _context.SeedProfileAsync(name: "Viewer", handle: "viewer");
        var mainTimetable = await _context.SeedTimetableAsync(
            ownerId,
            timetableId: roomId,
            roomId: roomId
        );
        _context.RoomMembers.AddRange(
            new RoomMember
            {
                RoomId = roomId,
                UserId = editorId,
                Role = RoomRole.Editor,
            },
            new RoomMember
            {
                RoomId = roomId,
                UserId = viewerId,
                Role = RoomRole.Viewer,
            }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        _roomTracker.SetRoom(
            roomId,
            new RoomInit([editorId], [viewerId], [mainTimetable.ToRoomTimetable()])
        );
        _roomTracker.RegisterConnection("owner-connection", ownerId);
        _roomTracker.MoveConnectionToRoom("owner-connection", ownerId, roomId);
        _roomTracker.RegisterConnection("editor-connection", editorId);
        _roomTracker.MoveConnectionToRoom("editor-connection", editorId, roomId);

        var result = await _service.GetRoomMembersAsync(roomId, ownerId);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result.Single(member => member.UserId == ownerId).Role.ShouldBe(RoomRole.Owner);
        result.Single(member => member.UserId == ownerId).IsInRoom.ShouldBeTrue();
        result.Single(member => member.UserId == editorId).Role.ShouldBe(RoomRole.Editor);
        result.Single(member => member.UserId == editorId).IsInRoom.ShouldBeTrue();
        result.Single(member => member.UserId == viewerId).Role.ShouldBe(RoomRole.Viewer);
        result.Single(member => member.UserId == viewerId).IsInRoom.ShouldBeFalse();
    }

    [Fact]
    public async Task GetRoomMembersAsync_ProfileHasAvatar_ReturnsAvatarUrlFromDatabaseState()
    {
        var roomId = Guid.NewGuid();
        var avatarUpdatedAt = AvatarUpdatedAt();
        var userId = await _context.SeedProfileAsync(avatarUpdatedAt: avatarUpdatedAt);
        var mainTimetable = await _context.SeedTimetableAsync(
            userId,
            timetableId: roomId,
            roomId: roomId
        );

        _profileTracker.SetUser(
            new Profile
            {
                Id = userId,
                Username = "Stale cached profile",
                Handle = "stale-cache",
                AvatarUpdatedAt = null,
            }
        );
        _roomTracker.SetRoom(roomId, new RoomInit([], [], [mainTimetable.ToRoomTimetable()]));

        var result = await _service.GetRoomMembersAsync(roomId, userId);

        result.ShouldNotBeNull();
        var member = result.Single();
        member.AvatarUrl.ShouldBe(TestAvatarUrlProvider.UrlFor(userId, avatarUpdatedAt));
        member.IsInRoom.ShouldBeFalse();
    }

    [Fact]
    public async Task GetRoomMembersAsync_NoRoomExistsInCache_ReturnsNull()
    {
        var res = await _service.GetRoomMembersAsync(Guid.NewGuid(), Guid.NewGuid());

        res.ShouldBeNull();
    }

    // === GetTimetablesDetailedInRoomAsync ===

    [Fact]
    public async Task GetTimetablesDetailedInRoomAsync_NoRoomExistsInCache_ReturnsNull()
    {
        var res = await _service.GetTimetablesDetailedInRoomAsync(Guid.NewGuid(), Guid.NewGuid());

        res.ShouldBeNull();
    }

    [Fact]
    public async Task GetTimetablesDetailedInRoomAsync_ReturnsTimetablesWithProfiles()
    {
        var roomId = Guid.NewGuid();
        var userId = await _context.SeedProfileAsync();
        var timetable = MakeTimetable(roomId, userId).ToRoomTimetable();

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [timetable]));

        var res = await _service.GetTimetablesDetailedInRoomAsync(roomId, userId);

        var profile = await _context.Profiles.FindAsync(userId);

        res.ShouldNotBeNull();
        res.ShouldMatch([
            timetable.ToDetailedResponse(_profileResponseMapper.ToResponse(profile!)),
        ]);
    }

    [Fact]
    public async Task GetTimetablesDetailedInRoomAsync_ProfileHasAvatar_ReturnsAvatarUrl()
    {
        var roomId = Guid.NewGuid();
        var avatarUpdatedAt = AvatarUpdatedAt();
        var userId = await _context.SeedProfileAsync(avatarUpdatedAt: avatarUpdatedAt);
        var timetable = MakeTimetable(roomId, userId).ToRoomTimetable();

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [timetable]));

        var result = await _service.GetTimetablesDetailedInRoomAsync(roomId, userId);

        result.ShouldNotBeNull();
        result
            .Single()
            .Profile.AvatarUrl.ShouldBe(TestAvatarUrlProvider.UrlFor(userId, avatarUpdatedAt));
    }

    [Fact]
    public async Task FindUsersByHandleAsync_ProfileHasAvatar_ReturnsAvatarUrl()
    {
        var roomId = Guid.NewGuid();
        var callerId = await _context.SeedProfileAsync(handle: "caller");
        var avatarUpdatedAt = AvatarUpdatedAt();
        var candidateId = await _context.SeedProfileAsync(
            name: "Alice",
            handle: "alice",
            avatarUpdatedAt: avatarUpdatedAt
        );

        _roomTracker.SetRoom(roomId, new RoomInit([callerId], [], []));

        var result = await _service.FindUsersByHandleAsync("ali", roomId, callerId);

        result.ShouldBe([
            new UserSearchResponse(
                candidateId,
                "Alice",
                "alice",
                TestAvatarUrlProvider.UrlFor(candidateId, avatarUpdatedAt)
            ),
        ]);
    }

    [Fact]
    public async Task GetTimetablesDetailedInRoomAsync_OwnerWithoutProfile_TimetableReturnedWithDeletedUser()
    {
        var roomId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId).ToRoomTimetable();

        _roomTracker.SetRoom(roomId, new RoomInit([viewerId], [], [timetable]));

        var res = await _service.GetTimetablesDetailedInRoomAsync(roomId, viewerId);

        res.ShouldNotBeNull();
        res.Count.ShouldBe(1);
        res.ElementAt(0).Profile.ShouldBe(ProfileResponse.DeletedUser);
    }

    // === RegisterConnectionAsync ===

    [Fact]
    public async Task RegisterConnectionAsync_ProfileInDatabase_AddsProfileToTracker()
    {
        var userId = await _context.SeedProfileAsync();

        await _service.RegisterConnectionAsync(userId, "connection");

        _profileTracker.TryGetUserById(userId, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterConnectionAsync_ProfileAlreadyInTracker_Succeeds()
    {
        var userId = Guid.NewGuid();
        _profileTracker.SetUser(new Profile { Id = userId, Username = "Cached" });

        await Should.NotThrowAsync(() => _service.RegisterConnectionAsync(userId, "connection"));
    }

    [Fact]
    public async Task RegisterConnectionAsync_NonExistentProfile_ThrowsNotFoundException()
    {
        await Should.ThrowAsync<NotFoundException>(() =>
            _service.RegisterConnectionAsync(Guid.NewGuid(), "connection")
        );
    }

    // === Room membership ===

    [Fact]
    public async Task SetMemberRoleAsync_ViewerToEditor_UpdatesSingleMembershipAndTrackerRole()
    {
        var ownerId = await _context.SeedProfileAsync();
        var memberId = await _context.SeedProfileAsync();
        var roomId = Guid.NewGuid();
        var mainTimetable = await _context.SeedTimetableAsync(
            ownerId,
            timetableId: roomId,
            roomId: roomId
        );
        _context.RoomMembers.Add(
            new RoomMember
            {
                RoomId = roomId,
                UserId = memberId,
                Role = RoomRole.Viewer,
            }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        _roomTracker.SetRoom(
            roomId,
            new RoomInit([], [memberId], [mainTimetable.ToRoomTimetable()])
        );

        await _service.SetMemberRoleAsync(roomId, memberId, RoomRole.Editor, ownerId);

        var membership = await _context.RoomMembers.SingleAsync(m =>
            m.RoomId == roomId && m.UserId == memberId
        );
        membership.Role.ShouldBe(RoomRole.Editor);
        _roomTracker.TryGetEditorsInRoom(roomId, out var editors).ShouldBeTrue();
        _roomTracker.TryGetViewersInRoom(roomId, out var viewers).ShouldBeTrue();
        editors.ShouldContain(memberId);
        viewers.ShouldNotContain(memberId);
    }

    [Fact]
    public async Task RevokeMemberAccessAsync_ConnectedMember_RemovesMembershipRoleAndPresence()
    {
        var ownerId = await _context.SeedProfileAsync();
        var memberId = await _context.SeedProfileAsync();
        var roomId = Guid.NewGuid();
        var mainTimetable = await _context.SeedTimetableAsync(
            ownerId,
            timetableId: roomId,
            roomId: roomId
        );
        _context.RoomMembers.Add(
            new RoomMember
            {
                RoomId = roomId,
                UserId = memberId,
                Role = RoomRole.Editor,
            }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        _roomTracker.SetRoom(
            roomId,
            new RoomInit([memberId], [], [mainTimetable.ToRoomTimetable()])
        );
        _roomTracker.RegisterConnection("owner-connection", ownerId);
        _roomTracker.MoveConnectionToRoom("owner-connection", ownerId, roomId);
        _roomTracker.RegisterConnection("member-connection-1", memberId);
        _roomTracker.MoveConnectionToRoom("member-connection-1", memberId, roomId);
        _roomTracker.RegisterConnection("member-connection-2", memberId);
        _roomTracker.MoveConnectionToRoom("member-connection-2", memberId, roomId);

        var removedConnectionIds = await _service.RevokeMemberAccessAsync(
            roomId,
            memberId,
            ownerId
        );

        (
            await _context.RoomMembers.AnyAsync(m => m.RoomId == roomId && m.UserId == memberId)
        ).ShouldBeFalse();
        _roomTracker.TryGetEditorsInRoom(roomId, out var editors).ShouldBeTrue();
        _roomTracker.TryGetUsersInRoom(roomId, out var users).ShouldBeTrue();
        editors.ShouldNotContain(memberId);
        users.ShouldNotContain(memberId);
        users.ShouldContain(ownerId);
        removedConnectionIds.ShouldBe(
            ["member-connection-1", "member-connection-2"],
            ignoreOrder: true
        );
    }

    // === CloseRoom ===

    [Fact]
    public async Task CloseRoom_ExistingRoom_RemovesRoomButRetainsConnectionScopedProfile()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _profileTracker.SetUser(new Profile { Id = userId, Username = "Test" });
        _roomTracker.SetRoom(roomId, new RoomInit([], [], []));

        var res = _service.CloseRoom(roomId);

        res.ShouldBeTrue();
        _roomTracker.RoomExists(roomId).ShouldBeFalse();
        _profileTracker.TryGetUserById(userId, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task CloseRoom_NonExistentRoom_ReturnsFalse()
    {
        var res = _service.CloseRoom(Guid.NewGuid());

        res.ShouldBeFalse();
    }

    // === HandleDeleteTimetable ===

    [Fact]
    public void HandleDeleteTimetable_MainTimetable_ReturnsFalse()
    {
        var roomId = Guid.NewGuid();

        _service.HandleDeleteTimetable(roomId, Guid.NewGuid(), roomId).ShouldBeFalse();
    }

    [Fact]
    public void HandleDeleteTimetable_NonMainTimetable_RemovesFromTracker()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId).ToRoomTimetable();

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], [timetable]));

        _service.HandleDeleteTimetable(roomId, userId, timetable.Id).ShouldBeTrue();

        _roomTracker.TryGetTimetablesInRoom(roomId, out var timetables).ShouldBeTrue();
        timetables.ShouldBeEmpty();
    }

    // === HandleUpdateTimetableAsync ===

    [Fact]
    public async Task HandleUpdateTimetableAsync_TimetableExistsInRoomTracker_UpdatesCorrectly()
    {
        var userId = Guid.NewGuid();
        var timetable = MakeTimetable().ToRoomTimetable();

        _roomTracker.SetRoom(timetable.RoomId, new RoomInit([userId], [], [timetable]));

        var update = new UpdateTimetableRequest
        {
            Name = "New Timetable",
            MetaData = CreateMetaData(),
        };

        var res = await _service.HandleUpdateTimetableAsync(
            timetable.RoomId,
            userId,
            timetable.Id,
            update
        );

        res.ShouldBeTrue();
        _roomTracker.TryGetTimetablesInRoom(timetable.RoomId, out var timetables).ShouldBeTrue();

        timetables.Count.ShouldBe(1);
        timetables.ElementAt(0).ShouldMatch(timetable.ApplyUpdate(update));
    }

    [Fact]
    public async Task HandleUpdateTimetableAsync_TimetableExistsInRoomTracker_AddsRoomToTracked()
    {
        var userId = Guid.NewGuid();
        var timetable = MakeTimetable().ToRoomTimetable();

        _roomTracker.SetRoom(timetable.RoomId, new RoomInit([userId], [], [timetable]));

        var update = new UpdateTimetableRequest
        {
            Name = "New Timetable",
            MetaData = CreateMetaData(),
        };

        var res = await _service.HandleUpdateTimetableAsync(
            timetable.RoomId,
            userId,
            timetable.Id,
            update
        );

        res.ShouldBeTrue();
        _roomTracker.TryGetChangedTimetables(timetable.RoomId, out var timetables);

        timetables.Count.ShouldBe(1);
        timetables.ElementAt(0).ShouldMatch(timetable.ApplyUpdate(update));
    }

    [Fact]
    public async Task HandleUpdateTimetableAsync_TimetableOnlyInDb_LoadsFromDbAndUpdatesCorrectly()
    {
        var userId = await _context.SeedProfileAsync();
        var roomId = await _context.SeedRoomAsync();
        var timetable = await _context.SeedTimetableAsync(userId, null, null, roomId);

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], []));

        var update = new UpdateTimetableRequest
        {
            Name = "New Timetable",
            MetaData = CreateMetaData(),
        };

        var res = await _service.HandleUpdateTimetableAsync(roomId, userId, timetable.Id, update);

        res.ShouldBeTrue();
        _roomTracker.TryGetTimetablesInRoom(roomId, out var timetables).ShouldBeTrue();

        timetables.Count.ShouldBe(1);
        timetables.ElementAt(0).ShouldMatch(timetable.ToRoomTimetable().ApplyUpdate(update));
    }

    [Fact]
    public async Task HandleUpdateTimetableAsync_TimetableOnlyInDb_AddsToChangedList()
    {
        var userId = await _context.SeedProfileAsync();
        var roomId = await _context.SeedRoomAsync();
        var timetable = await _context.SeedTimetableAsync(userId, null, null, roomId);

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], []));

        var update = new UpdateTimetableRequest
        {
            Name = "New Timetable",
            MetaData = CreateMetaData(),
        };

        var res = await _service.HandleUpdateTimetableAsync(roomId, userId, timetable.Id, update);

        res.ShouldBeTrue();
        _roomTracker.TryGetChangedTimetables(roomId, out var timetables).ShouldBeTrue();

        timetables.Count.ShouldBe(1);
        timetables.ElementAt(0).ShouldMatch(timetable.ToRoomTimetable().ApplyUpdate(update));
    }

    [Fact]
    public async Task HandleUpdateTimetableAsync_TimetableDoesNotExist_ReturnsFalse()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _roomTracker.SetRoom(roomId, new RoomInit([userId], [], []));

        var res = await _service.HandleUpdateTimetableAsync(
            roomId,
            userId,
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

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [unchangedTimetable]));
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

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [unchangedTimetable]));
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

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [unchangedTimetable]));
        _roomTracker.AddOrUpdateTimetable(timetable);

        var success = await _service.CommitChangesAsync(roomId);
        success.ShouldBeTrue();

        _roomTracker.TryGetChangedTimetables(roomId, out var timetables).ShouldBeTrue();
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
            new RoomInit([], [], [deleted.ToRoomTimetable(), kept.ToRoomTimetable()])
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

        _roomTracker.SetRoom(roomId, new RoomInit([], [], [unchangedTimetable, timetable]));
        _roomTracker.DeleteTimetable(roomId, timetable.Id);

        var success = await _service.CommitChangesAsync(roomId);
        success.ShouldBeTrue();

        _roomTracker.TryGetChangedTimetables(roomId, out var timetables).ShouldBeTrue();
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
            new RoomInit([], [], [mainTimetable.ToRoomTimetable()])
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

    private static DateTime AvatarUpdatedAt() => new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

    private sealed class TestAvatarUrlProvider : IAvatarUrlProvider
    {
        public string? GetAvatarUrl(Profile profile) =>
            profile.AvatarUpdatedAt is { } updatedAt ? UrlFor(profile.Id, updatedAt) : null;

        public static string UrlFor(Guid userId, DateTime updatedAt) =>
            $"https://avatars.test/{userId}/avatar.webp?v={updatedAt.Ticks}";
    }
}
