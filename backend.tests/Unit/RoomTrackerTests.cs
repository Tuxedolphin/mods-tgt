using Backend.DTOs;
using Backend.Exceptions;
using Backend.Models;
using Backend.Services.Rooms;
using Shouldly;

namespace Backend.Tests.Unit;

public class RoomTrackerTests
{
    private readonly RoomTracker _tracker = new();

    // === AddRoom ===

    [Fact]
    public void AddRoom_NewRoom_ReturnsTrue()
    {
        var result = _tracker.AddRoom(Guid.NewGuid());
        result.ShouldBeTrue();
    }

    [Fact]
    public void AddRoom_DuplicateRoom_ReturnsFalse()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        var result = _tracker.AddRoom(roomId);

        result.ShouldBeFalse();
    }

    // === RoomExists ===

    [Fact]
    public void RoomExists_GetExistingRoom_ReturnsTrue()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        var result = _tracker.RoomExists(roomId);

        result.ShouldBeTrue();
    }

    [Fact]
    public void RoomExists_GetNonExistingRoom_ReturnsFalse()
    {
        _tracker.AddRoom(Guid.NewGuid());
        var result = _tracker.RoomExists(Guid.NewGuid());

        result.ShouldBeFalse();
    }

    [Fact]
    public void RoomExists_GetRoomFromEmptyTracker_ReturnsFalse()
    {
        var result = _tracker.RoomExists(Guid.NewGuid());
        result.ShouldBeFalse();
    }

    // === Connection room membership ===

    [Fact]
    public void Visibility_DefinesCurrentVisibilityModes()
    {
        Enum.GetValues<Visibility>()
            .ShouldBe([Visibility.PublicView, Visibility.PublicEdit, Visibility.Restricted]);
    }

    [Fact]
    public void MoveConnectionToRoom_PublicRoom_AllowsNonMember()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string connectionId = "connection";

        _tracker.SetRoom(roomId, new RoomInit([], [], [], Visibility.PublicView));
        _tracker.RegisterConnection(connectionId, userId);

        Should.NotThrow(() => _tracker.MoveConnectionToRoom(connectionId, userId, roomId));
    }

    [Fact]
    public void MoveConnectionToRoom_RestrictedRoom_RejectsNonMember()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string connectionId = "connection";

        _tracker.SetRoom(roomId, new RoomInit([], [], [], Visibility.Restricted));
        _tracker.RegisterConnection(connectionId, userId);

        Should.Throw<ForbiddenException>(() =>
            _tracker.MoveConnectionToRoom(connectionId, userId, roomId)
        );
    }

    [Fact]
    public void MoveConnectionToRoom_ExistingRoom_AssociatesConnectionWithRoom()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string connectionId = "connection";

        _tracker.AddRoom(roomId);
        _tracker.SetMemberRole(roomId, userId, RoomRole.Viewer);
        _tracker.RegisterConnection(connectionId, userId);
        _tracker.MoveConnectionToRoom(connectionId, userId, roomId);

        _tracker.TryGetRoomOfConnection(connectionId, out var currentRoomId).ShouldBeTrue();
        currentRoomId.ShouldBe(roomId);
    }

    [Fact]
    public void MoveConnectionToRoom_NonExistingRoom_Throws()
    {
        var userId = Guid.NewGuid();
        const string connectionId = "connection";
        _tracker.RegisterConnection(connectionId, userId);

        Should.Throw<InvalidOperationException>(() =>
            _tracker.MoveConnectionToRoom(connectionId, userId, Guid.NewGuid())
        );
    }

    [Fact]
    public void MoveConnectionToRoom_RegisteredConnection_AssociatesConnectionWithRoom()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        const string connectionId = "connection";
        AddConnectionToRoom(roomId, userId, connectionId);

        _tracker.TryGetRoomOfConnection(connectionId, out var resultRoomId).ShouldBeTrue();
        resultRoomId.ShouldBe(roomId);
    }

    [Fact]
    public void MoveConnectionToRoom_ConnectionRejoinsSameRoom_ReturnsSameRoom()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        const string connectionId = "connection";
        AddConnectionToRoom(roomId, userId, connectionId);
        var result = _tracker.MoveConnectionToRoom(connectionId, userId, roomId);

        result.PreviousRoomId.ShouldBe(roomId);
    }

    [Fact]
    public void MoveConnectionToRoom_ConnectionInDifferentRoom_ReturnsPreviousRoomAndMovesConnection()
    {
        var userId = Guid.NewGuid();
        var initialRoomId = Guid.NewGuid();
        var newRoomId = Guid.NewGuid();

        const string connectionId = "connection";
        AddConnectionToRoom(initialRoomId, userId, connectionId);
        _tracker.AddRoom(newRoomId);
        _tracker.SetMemberRole(newRoomId, userId, RoomRole.Viewer);
        var move = _tracker.MoveConnectionToRoom(connectionId, userId, newRoomId);

        move.PreviousRoomId.ShouldBe(initialRoomId);
        _tracker.TryGetRoomOfConnection(connectionId, out var currentRoomId).ShouldBeTrue();
        currentRoomId.ShouldBe(newRoomId);
    }

    [Fact]
    public void MoveConnectionToRoom_UserHasAnotherConnection_PreservesPreviousRoomPresence()
    {
        var userId = Guid.NewGuid();
        var initialRoomId = Guid.NewGuid();
        var newRoomId = Guid.NewGuid();

        const string connectionId = "connection";
        AddConnectionToRoom(initialRoomId, userId, connectionId);
        AddConnectionToRoom(initialRoomId, userId, "other-connection");
        _tracker.AddRoom(newRoomId);
        _tracker.SetMemberRole(newRoomId, userId, RoomRole.Viewer);
        _tracker.MoveConnectionToRoom(connectionId, userId, newRoomId);

        _tracker.TryGetUsersInRoom(initialRoomId, out var users);
        _tracker.TryGetRoomOfConnection(connectionId, out var resultRoomId).ShouldBeTrue();

        users.ShouldBe([userId]);
        resultRoomId.ShouldBe(newRoomId);
    }

    // === GetRoomOfConnection ===

    [Fact]
    public void TryGetRoomOfConnection_GetFromEmptyTracker_ReturnsFalse()
    {
        var result = _tracker.TryGetRoomOfConnection("missing", out _);
        result.ShouldBeFalse();
    }

    [Fact]
    public void TryGetRoomOfConnection_GetNonExistingConnection_ReturnsFalse()
    {
        _tracker.AddRoom(Guid.NewGuid());
        var result = _tracker.TryGetRoomOfConnection("missing", out _);
        result.ShouldBeFalse();
    }

    [Fact]
    public void TryGetRoomOfConnection_GetDifferentConnectionFromAdded_ReturnsFalse()
    {
        var roomId = Guid.NewGuid();

        AddConnectionToRoom(roomId, Guid.NewGuid(), "existing");

        var result = _tracker.TryGetRoomOfConnection("missing", out _);
        result.ShouldBeFalse();
    }

    [Fact]
    public void TryGetRoomOfConnection_GetExistingConnection_ReturnsTrueAndCorrectRoomId()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        const string connectionId = "connection";
        AddConnectionToRoom(roomId, userId, connectionId);
        var result = _tracker.TryGetRoomOfConnection(connectionId, out var resultRoomId);

        result.ShouldBeTrue();
        resultRoomId.ShouldBe(roomId);
    }

    [Fact]
    public void TryGetRoomOfConnection_AfterRepeatedJoin_ReturnsCorrectRoomId()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        const string connectionId = "connection";
        AddConnectionToRoom(roomId, userId, connectionId);
        _tracker.MoveConnectionToRoom(connectionId, userId, roomId);
        var result = _tracker.TryGetRoomOfConnection(connectionId, out var resultRoomId);

        result.ShouldBeTrue();
        resultRoomId.ShouldBe(roomId);
    }

    // === GetTimetablesInRoom ===

    [Fact]
    public void TryGetTimetablesInRoom_GetNonExistingRoom_ReturnsFalse()
    {
        var result = _tracker.TryGetTimetablesInRoom(Guid.NewGuid(), out _);

        result.ShouldBeFalse();
    }

    [Fact]
    public void TryGetTimetablesInRoom_GetFromEmptyRoom_ReturnsTrue()
    {
        var roomId = Guid.NewGuid();
        _tracker.AddRoom(roomId);

        var result = _tracker.TryGetTimetablesInRoom(roomId, out var timetables);

        result.ShouldBeTrue();
        timetables.ShouldBe([]);
    }

    [Fact]
    public void TryGetTimetablesInRoom_GetFromExistingRoom_ReturnsCorrectList()
    {
        var roomId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId);

        _tracker.AddRoom(roomId);
        _tracker.AddOrUpdateTimetable(timetable);

        var result = _tracker.TryGetTimetablesInRoom(roomId, out var timetables);

        result.ShouldBeTrue();
        timetables.ShouldBe([timetable]);
    }

    // === GetUsersInRoom ===

    [Fact]
    public void TryGetUsersInRoom_GetNonExistingRoom_ReturnsFalse()
    {
        var result = _tracker.TryGetUsersInRoom(Guid.NewGuid(), out _);

        result.ShouldBeFalse();
    }

    [Fact]
    public void TryGetUsersInRoom_GetFromEmptyRoom_ReturnsTrue()
    {
        var roomId = Guid.NewGuid();
        _tracker.AddRoom(roomId);

        var result = _tracker.TryGetUsersInRoom(roomId, out var users);

        result.ShouldBeTrue();
        users.ShouldBe([]);
    }

    [Fact]
    public void TryGetUsersInRoom_GetFromExistingRoom_ReturnsCorrectList()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        AddConnectionToRoom(roomId, userId, "connection");
        AddConnectionToRoom(roomId, userId, "other-connection");

        var result = _tracker.TryGetUsersInRoom(roomId, out var users);

        result.ShouldBeTrue();
        users.ShouldBe([userId]);
    }

    // === LeaveConnectionFromRoom ===

    [Fact]
    public void LeaveConnectionFromRoom_EmptyRoom_ReturnsNull()
    {
        var roomId = Guid.NewGuid();
        _tracker.AddRoom(roomId);

        var result = _tracker.LeaveConnectionFromRoom("missing", roomId);

        result.ShouldBeNull();
    }

    [Fact]
    public void LeaveConnectionFromRoom_NonExistingConnection_ReturnsNull()
    {
        var result = _tracker.LeaveConnectionFromRoom("missing", Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public void LeaveConnectionFromRoom_UserHasAnotherConnection_PreservesUserPresence()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        const string connectionId = "connection";
        AddConnectionToRoom(roomId, userId, connectionId);
        AddConnectionToRoom(roomId, userId, "other-connection");

        var departure = _tracker.LeaveConnectionFromRoom(connectionId, roomId);
        _tracker.TryGetUsersInRoom(roomId, out var users);

        users.ShouldBe([userId]);
        departure.ShouldNotBeNull();
        departure.UserId.ShouldBe(userId);
    }

    [Fact]
    public void LeaveConnectionFromRoom_ExistingRoom_RoomRemainsOpen()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        const string connectionId = "connection";
        AddConnectionToRoom(roomId, userId, connectionId);

        var departure = _tracker.LeaveConnectionFromRoom(connectionId, roomId);

        departure.ShouldNotBeNull();
        _tracker.RoomExists(roomId).ShouldBeTrue();
    }

    [Fact]
    public void SetRoom_WithEditorsAndViewers_PopulatesBothAtomically()
    {
        var roomId = Guid.NewGuid();
        IReadOnlyCollection<Guid> editors = [Guid.NewGuid(), Guid.NewGuid()];
        IReadOnlyCollection<Guid> viewers = [Guid.NewGuid()];

        _tracker.SetRoom(roomId, new RoomInit(editors, viewers, []));

        _tracker.TryGetEditorsInRoom(roomId, out var editorsRes).ShouldBeTrue();
        _tracker.TryGetViewersInRoom(roomId, out var viewersRes).ShouldBeTrue();

        editorsRes.ShouldBe(editors, ignoreOrder: true);
        viewersRes.ShouldBe(viewers, ignoreOrder: true);
    }

    // === Member roles ===

    [Fact]
    public void SetMemberRole_NonExistingRoom_ReturnsFalse()
    {
        var result = _tracker.SetMemberRole(
            Guid.NewGuid(),
            Guid.NewGuid(),
            RoomRole.Editor
        );
        result.ShouldBeFalse();
    }

    [Fact]
    public void SetMemberRole_EditorToViewer_MovesRoleAtomically()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        _tracker.SetMemberRole(roomId, userId, RoomRole.Editor);
        var result = _tracker.SetMemberRole(roomId, userId, RoomRole.Viewer);

        result.ShouldBeTrue();
        _tracker.TryGetEditorsInRoom(roomId, out var editors).ShouldBeTrue();
        _tracker.TryGetViewersInRoom(roomId, out var viewers).ShouldBeTrue();
        editors.ShouldNotContain(userId);
        viewers.ShouldContain(userId);
    }

    [Fact]
    public void RemoveMemberRoleAndConnections_ConnectedMember_RemovesRoleAndRoomConnections()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        AddConnectionToRoom(roomId, userId, "connection-1");
        AddConnectionToRoom(roomId, userId, "connection-2");
        _tracker.SetMemberRole(roomId, userId, RoomRole.Editor);

        var result = _tracker.RemoveMemberRoleAndConnections(roomId, userId);

        result.ShouldBe(["connection-1", "connection-2"], ignoreOrder: true);
        _tracker.TryGetEditorsInRoom(roomId, out var editors).ShouldBeTrue();
        _tracker.TryGetViewersInRoom(roomId, out var viewers).ShouldBeTrue();
        _tracker.TryGetUsersInRoom(roomId, out var users).ShouldBeTrue();
        editors.ShouldBeEmpty();
        viewers.ShouldBeEmpty();
        users.ShouldBeEmpty();
    }

    // === CloseRoom clears roles ===

    [Fact]
    public void CloseRoom_ClearsEditorsAndViewers()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        _tracker.SetMemberRole(roomId, Guid.NewGuid(), RoomRole.Editor);
        _tracker.SetMemberRole(roomId, Guid.NewGuid(), RoomRole.Viewer);

        _tracker.CloseRoom(roomId);

        _tracker.TryGetEditorsInRoom(roomId, out _).ShouldBeFalse();
        _tracker.TryGetViewersInRoom(roomId, out _).ShouldBeFalse();
    }

    // === AddOrUpdateTimetable

    [Fact]
    public void AddOrUpdateTimetable_AddTimetableToNonExistingRoom_ReturnsFalse()
    {
        var result = _tracker.AddOrUpdateTimetable(MakeTimetable());
        result.ShouldBeFalse();
    }

    [Fact]
    public void AddOrUpdateTimetable_AddTimetableToExistingRoom_ReturnsTrue()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        var result = _tracker.AddOrUpdateTimetable(MakeTimetable(roomId));

        result.ShouldBeTrue();
    }

    [Fact]
    public void AddOrUpdateTimetable_AddTimetableToExistingRoom_AddsTimetableToList()
    {
        var roomId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId);

        _tracker.AddRoom(roomId);
        _tracker.AddOrUpdateTimetable(timetable);

        var result = _tracker.GetTimetableById(roomId, timetable.Id);
        result.ShouldBe(timetable);
    }

    [Fact]
    public void AddOrUpdateTimetable_UpdateTimetableInExistingRoom_UpdatesRelevantTimetable()
    {
        var roomId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId);

        const string changedName = "Name has been changed!";

        _tracker.AddRoom(roomId);
        _tracker.AddOrUpdateTimetable(timetable);

        var updated = new RoomTimetable
        {
            Id = timetable.Id,
            Name = changedName,
            RoomId = timetable.RoomId,
            UserId = timetable.UserId,
            Semester = timetable.Semester,
            AcademicYear = timetable.AcademicYear,
            CreatedAt = timetable.CreatedAt,
            MetaData = [],
        };
        _tracker.AddOrUpdateTimetable(updated);

        var result = _tracker.GetTimetableById(roomId, timetable.Id);

        result.ShouldNotBeNull();
        result.Name.ShouldBe(changedName);
    }

    [Fact]
    public void AddOrUpdateTimetable_UpdateTimetableInExistingRoom_AddsTimetableToChangesTimetablesList()
    {
        var roomId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId);

        _tracker.AddRoom(roomId);
        _tracker.AddOrUpdateTimetable(timetable);

        var success = _tracker.TryGetChangedTimetables(roomId, out var changedTimetables);

        success.ShouldBeTrue();
        changedTimetables.Contains(timetable).ShouldBeTrue();
    }

    // === GetTimetableById ===

    [Fact]
    public void GetTimetableById_GetFromNonExistingRoom_ReturnsNull()
    {
        var result = _tracker.GetTimetableById(Guid.NewGuid(), Guid.NewGuid());
        result.ShouldBeNull();
    }

    [Fact]
    public void GetTimetableById_GetExistingTimetable_ReturnsTimetable()
    {
        var roomId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId);

        _tracker.AddRoom(roomId);
        _tracker.AddOrUpdateTimetable(timetable);

        var result = _tracker.GetTimetableById(roomId, timetable.Id);
        result.ShouldBe(timetable);
    }

    [Fact]
    public void GetTimetableById_GetExistingTimetableFromDifferentRoom_ReturnsNull()
    {
        var roomId = Guid.NewGuid();
        var otherRoomId = Guid.NewGuid();

        var timetable = MakeTimetable(roomId);

        _tracker.AddRoom(roomId);
        _tracker.AddRoom(otherRoomId);
        _tracker.AddOrUpdateTimetable(timetable);

        var result = _tracker.GetTimetableById(otherRoomId, timetable.Id);

        result.ShouldBeNull();
    }

    // === DeleteTimetable ===

    [Fact]
    public void DeleteTimetable_DeleteOnNonExistingRoom_ReturnsFalse()
    {
        var result = _tracker.DeleteTimetable(Guid.NewGuid(), Guid.NewGuid());
        result.ShouldBeFalse();
    }

    [Fact]
    public void DeleteTimetable_DeleteOnEmptyRoom_ReturnsTrue()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        var result = _tracker.DeleteTimetable(roomId, Guid.NewGuid());

        result.ShouldBeTrue();
    }

    [Fact]
    public void DeleteTimetable_DeleteExistingTimetable_RemovesTimetableFromCache()
    {
        var roomId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId);

        _tracker.AddRoom(roomId);
        _tracker.AddOrUpdateTimetable(timetable);

        var success = _tracker.DeleteTimetable(roomId, timetable.Id);
        var exists = _tracker.GetTimetableById(roomId, timetable.Id);

        success.ShouldBeTrue();
        exists.ShouldBeNull();
    }

    [Fact]
    public void DeleteTimetable_DeleteExistingTimetable_AddsTimetableToDeletedList()
    {
        var roomId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId);

        _tracker.AddRoom(roomId);
        _tracker.AddOrUpdateTimetable(timetable);

        var success = _tracker.DeleteTimetable(roomId, timetable.Id);
        success.ShouldBeTrue();

        _tracker.GetDeletedTimetables(roomId).Contains(timetable.Id).ShouldBeTrue();
    }

    [Fact]
    public void DeleteTimetable_TimetablePreviouslyChanged_RemovesFromChangedAndAddsToDeleted()
    {
        var roomId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId);

        _tracker.AddRoom(roomId);
        _tracker.AddOrUpdateTimetable(timetable);

        _tracker.DeleteTimetable(roomId, timetable.Id);

        _tracker.GetDeletedTimetables(roomId).ShouldContain(timetable.Id);

        _tracker.TryGetChangedTimetables(roomId, out var changed);
        changed.ShouldNotContain(timetable);
    }

    // === SetRoom ===

    [Fact]
    public void SetRoom_SetRoomWithEmpty_SetsCorrectly()
    {
        var roomId = Guid.NewGuid();

        _tracker.SetRoom(roomId, new RoomInit([], [], []));

        _tracker.TryGetTimetablesInRoom(roomId, out var timetables).ShouldBeTrue();
        _tracker.TryGetUsersInRoom(roomId, out var users).ShouldBeTrue();

        timetables.ShouldBeEmpty();
        users.ShouldBeEmpty();
    }

    [Fact]
    public void SetRoom_SetRoomWithFilledLists_SetsCorrectly()
    {
        var roomId = Guid.NewGuid();

        IReadOnlyCollection<Guid> users = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        IReadOnlyCollection<RoomTimetable> timetables =
        [
            MakeTimetable(roomId),
            MakeTimetable(roomId),
        ];

        _tracker.SetRoom(roomId, new RoomInit([], users, timetables));
        foreach (var userId in users)
            AddConnectionToRoom(roomId, userId, userId.ToString());

        _tracker.TryGetTimetablesInRoom(roomId, out var timetablesRes).ShouldBeTrue();
        _tracker.TryGetUsersInRoom(roomId, out var usersRes).ShouldBeTrue();

        timetablesRes.ShouldBe(timetables, ignoreOrder: true);
        usersRes.ShouldBe(users, ignoreOrder: true);
    }

    [Fact]
    public void SetRoom_SetRoomOnExistingRoom_ReturnsFalse()
    {
        var roomId = Guid.NewGuid();

        _tracker.SetRoom(roomId, new RoomInit([], [], []));
        var res = _tracker.SetRoom(roomId, new RoomInit([], [], []));

        res.ShouldBeFalse();
    }

    [Fact]
    public void SetRoom_SetRoomWithWrongTimetableRoomId_ThrowsArgumentException()
    {
        var roomId = Guid.NewGuid();

        IReadOnlyCollection<Guid> users = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        IReadOnlyCollection<RoomTimetable> timetables =
        [
            MakeTimetable(roomId),
            MakeTimetable(Guid.NewGuid()),
        ];

        Action act = () => _tracker.SetRoom(roomId, new RoomInit([], [], timetables));

        act.ShouldThrow<ArgumentException>();
    }

    // === CloseRoom ===

    [Fact]
    public void CloseRoom_CloseNonExistentRoom_ReturnsFalse()
    {
        var result = _tracker.CloseRoom(Guid.NewGuid());

        result.ShouldBeFalse();
    }

    [Fact]
    public void CloseRoom_CloseExistingRoom_ReturnsTrue()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        var result = _tracker.CloseRoom(roomId);

        result.ShouldBeTrue();
    }

    [Fact]
    public void CloseRoom_CloseExistingRoom_RemovesTimetablesFromCache()
    {
        var roomId = Guid.NewGuid();

        IReadOnlyCollection<RoomTimetable> timetables =
        [
            MakeTimetable(roomId),
            MakeTimetable(roomId),
        ];

        _tracker.SetRoom(roomId, new RoomInit([], [], timetables));
        _tracker.CloseRoom(roomId);

        _tracker.TryGetTimetablesInRoom(roomId, out _).ShouldBeFalse();
        _tracker.TryGetUsersInRoom(roomId, out _).ShouldBeFalse();

    }

    [Fact]
    public void CloseRoom_CloseExistingChangedRoom_RemovesChangedTimetablesFromCache()
    {
        var roomId = Guid.NewGuid();

        IReadOnlyCollection<Guid> users = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        List<RoomTimetable> timetables = [MakeTimetable(roomId), MakeTimetable(roomId)];

        timetables[0].Name = "Changed!";

        _tracker.SetRoom(roomId, new RoomInit([], [], timetables));
        _tracker.AddOrUpdateTimetable(timetables[0]);

        _tracker.CloseRoom(roomId);

        _tracker.TryGetChangedTimetables(roomId, out _).ShouldBeFalse();
    }

    // === GetChangedTimetables ===

    [Fact]
    public void TryGetChangedTimetables_GetOnNonExistingRoom_ReturnsFalse()
    {
        var result = _tracker.TryGetChangedTimetables(Guid.NewGuid(), out _);

        result.ShouldBeFalse();
    }

    [Fact]
    public void TryGetChangedTimetables_GetOnExistingRoom_ReturnsEmpty()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        _tracker.TryGetChangedTimetables(roomId, out var res).ShouldBeTrue();

        res.ShouldBeEmpty();
    }

    [Fact]
    public void TryGetChangedTimetables_GetOnExistingRoom_ReturnsCorrectChanged()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);

        IReadOnlyCollection<Guid> users = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        List<RoomTimetable> timetables = [MakeTimetable(roomId), MakeTimetable(roomId)];

        _tracker.SetRoom(roomId, new RoomInit([], [], timetables));

        var changedTimetable = timetables[0];
        changedTimetable.Name = "Changed!";
        _tracker.AddOrUpdateTimetable(changedTimetable);

        _tracker.TryGetChangedTimetables(roomId, out var res).ShouldBeTrue();
        res.ShouldContain(changedTimetable);
    }

    // === RemoveTimetablesFromChanged ===

    [Fact]
    public void RemoveTimetablesFromChanged_RemoveOnEmptyList_ReturnsEmpty()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);

        var res = _tracker.RemoveTimetablesFromChanged([]);
        res.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveTimetablesFromChanged_RemoveOnNonExistentRoom_ReturnsEmpty()
    {
        var res = _tracker.RemoveTimetablesFromChanged([]);
        res.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveTimetablesFromChanged_InputHasIds_ButChangedSetIsEmpty_ReturnsInputUnchanged()
    {
        IReadOnlyCollection<Guid> timetables = [Guid.NewGuid(), Guid.NewGuid()];

        _tracker.AddRoom(Guid.NewGuid());

        var res = _tracker.RemoveTimetablesFromChanged(timetables);
        res.ShouldBe(timetables, ignoreOrder: true);
    }

    [Fact]
    public void RemoveTimetablesFromChanged_RemoveFilledFromNonExistentRoom_ReturnsOriginal()
    {
        IReadOnlyCollection<Guid> timetables = [Guid.NewGuid(), Guid.NewGuid()];

        var res = _tracker.RemoveTimetablesFromChanged(timetables);
        res.ShouldBe(timetables, ignoreOrder: true);
    }

    [Fact]
    public void RemoveTimetablesFromChanged_RemoveFromList_RemovesFromChangedList()
    {
        var roomId = Guid.NewGuid();

        IReadOnlyCollection<RoomTimetable> timetables =
        [
            MakeTimetable(roomId),
            MakeTimetable(roomId),
        ];

        _tracker.SetRoom(roomId, new RoomInit([], [], timetables));

        var changed = timetables.First();
        changed.Name = "This has been changed!";
        _tracker.AddOrUpdateTimetable(changed);

        var secondChanged = timetables.ElementAt(1);
        secondChanged.Name = "This has also been changed!";
        _tracker.AddOrUpdateTimetable(secondChanged);

        var res = _tracker.RemoveTimetablesFromChanged([changed.Id]);
        res.ShouldBeEmpty();

        _tracker.TryGetChangedTimetables(roomId, out var changedRes).ShouldBeTrue();
        changedRes.ShouldBe([secondChanged]);
    }

    // === RemoveTimetableFromDeleted ===

    [Fact]
    public void RemoveTimetablesFromDeleted_RemoveOnEmptyList_ReturnsEmpty()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);

        var res = _tracker.RemoveTimetablesFromDeleted(roomId, []);
        res.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveTimetablesFromDeleted_RemoveOnNonExistentRoom_ReturnsEmpty()
    {
        var res = _tracker.RemoveTimetablesFromDeleted(Guid.NewGuid(), []);
        res.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveTimetablesFromDeleted_RemoveFilledFromEmptyList_ReturnsOriginal()
    {
        IReadOnlyCollection<Guid> timetables = [Guid.NewGuid(), Guid.NewGuid()];

        var roomId = Guid.NewGuid();
        _tracker.AddRoom(roomId);

        var res = _tracker.RemoveTimetablesFromDeleted(roomId, timetables);
        res.ShouldBe(timetables, ignoreOrder: true);
    }

    [Fact]
    public void RemoveTimetablesFromDeleted_RemoveFilledFromNonExistentRoom_ReturnsOriginal()
    {
        IReadOnlyCollection<Guid> timetables = [Guid.NewGuid(), Guid.NewGuid()];

        var res = _tracker.RemoveTimetablesFromDeleted(Guid.NewGuid(), timetables);
        res.ShouldBe(timetables, ignoreOrder: true);
    }

    [Fact]
    public void RemoveTimetablesFromDeleted_RemoveFromList_RemovesFromDeletedList()
    {
        var roomId = Guid.NewGuid();

        IReadOnlyCollection<RoomTimetable> timetables =
        [
            MakeTimetable(roomId),
            MakeTimetable(roomId),
        ];

        _tracker.SetRoom(roomId, new RoomInit([], [], timetables));
        _tracker.DeleteTimetable(roomId, timetables.ElementAt(0).Id);
        _tracker.DeleteTimetable(roomId, timetables.ElementAt(1).Id);

        var res = _tracker.RemoveTimetablesFromDeleted(roomId, [timetables.ElementAt(0).Id]);
        res.ShouldBeEmpty();

        _tracker.RemoveTimetablesFromDeleted(roomId, [timetables.ElementAt(0).Id]);

        var deletedRes = _tracker.GetDeletedTimetables(roomId);
        deletedRes.ShouldBe([timetables.ElementAt(1).Id]);
    }

    private static RoomTimetable MakeTimetable(Guid? roomId = null, string name = "Test") =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            RoomId = roomId ?? Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Semester = 1,
            AcademicYear = "2024-2025",
            CreatedAt = DateTime.UtcNow,
            MetaData = [],
        };

    private void AddConnectionToRoom(Guid roomId, Guid userId, string connectionId)
    {
        if (!_tracker.RoomExists(roomId))
            _tracker.AddRoom(roomId);

        _tracker.SetMemberRole(roomId, userId, RoomRole.Viewer);
        _tracker.RegisterConnection(connectionId, userId);
        _tracker.MoveConnectionToRoom(connectionId, userId, roomId);
    }
}
