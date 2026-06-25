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

    // === AddUserToRoom ===

    [Fact]
    public void AddUserToRoom_ExistingRoom_ReturnsTrue()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        var result = _tracker.AddUserToRoom(Guid.NewGuid(), roomId);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AddUserToRoom_NonExistingRoom_ReturnsFalse()
    {
        var result = _tracker.AddUserToRoom(Guid.NewGuid(), Guid.NewGuid());
        result.ShouldBeFalse();
    }

    [Fact]
    public void AddUserToRoom_AddsUserToRoom_ReturnsTrueAndCorrectRoomId()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        _tracker.AddUserToRoom(userId, roomId);

        _tracker.GetRoomOfUser(userId, out var resultRoomId).ShouldBeTrue();
        resultRoomId.ShouldBe(roomId);
    }

    [Fact]
    public void AddUserToRoom_UserRejoinsSameRoom_ReturnsTrue()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        _tracker.AddUserToRoom(userId, roomId);
        var result = _tracker.AddUserToRoom(userId, roomId);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AddUserToRoom_UserInDifferentRoom_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var initialRoomId = Guid.NewGuid();
        var newRoomId = Guid.NewGuid();

        _tracker.AddRoom(initialRoomId);
        _tracker.AddRoom(newRoomId);
        _tracker.AddUserToRoom(userId, initialRoomId);
        var result = _tracker.AddUserToRoom(userId, newRoomId);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AddUserToRoom_UserInDifferentRoom_MovesUserToNewRoom()
    {
        var userId = Guid.NewGuid();
        var initialRoomId = Guid.NewGuid();
        var newRoomId = Guid.NewGuid();

        _tracker.AddRoom(initialRoomId);
        _tracker.AddRoom(newRoomId);

        _tracker.AddUserToRoom(Guid.NewGuid(), initialRoomId); // We do this as the room will auto close with no one
        _tracker.AddUserToRoom(userId, initialRoomId);
        _tracker.AddUserToRoom(userId, newRoomId);

        _tracker.GetUsersInRoom(initialRoomId, out var users);
        _tracker.GetRoomOfUser(userId, out var resultRoomId).ShouldBeTrue();

        var userInInitialRoom = users.Contains(userId);

        userInInitialRoom.ShouldBeFalse();
        resultRoomId.ShouldBe(newRoomId);
    }

    // === GetRoomOfUser ===

    [Fact]
    public void GetRoomOfUser_GetFromEmptyTracker_ReturnsFalse()
    {
        var result = _tracker.GetRoomOfUser(Guid.NewGuid(), out _);
        result.ShouldBeFalse();
    }

    [Fact]
    public void GetRoomOfUser_GetNonExistingUser_ReturnsFalse()
    {
        _tracker.AddRoom(Guid.NewGuid());
        var result = _tracker.GetRoomOfUser(Guid.NewGuid(), out _);
        result.ShouldBeFalse();
    }

    [Fact]
    public void GetRoomOfUser_GetDifferentUserFromAdded_ReturnsFalse()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        _tracker.AddUserToRoom(Guid.NewGuid(), roomId);

        var result = _tracker.GetRoomOfUser(Guid.NewGuid(), out _);
        result.ShouldBeFalse();
    }

    [Fact]
    public void GetRoomOfUser_GetExistingUser_ReturnsTrueAndCorrectRoomId()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        _tracker.AddUserToRoom(userId, roomId);
        var result = _tracker.GetRoomOfUser(userId, out var resultRoomId);

        result.ShouldBeTrue();
        resultRoomId.ShouldBe(roomId);
    }

    [Fact]
    public void GetRoomOfUser_GetExistingUserAfterUserJoinedMultipleTimes_ReturnsTrueAndCorrectRoomId()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        _tracker.AddUserToRoom(userId, roomId);
        _tracker.AddUserToRoom(userId, roomId);
        var result = _tracker.GetRoomOfUser(userId, out var resultRoomId);

        result.ShouldBeTrue();
        resultRoomId.ShouldBe(roomId);
    }

    // === GetTimetablesInRoom ===

    [Fact]
    public void GetTimetablesInRoom_GetNonExistingRoom_ReturnsFalse()
    {
        var result = _tracker.GetTimetablesInRoom(Guid.NewGuid(), out _);

        result.ShouldBeFalse();
    }

    [Fact]
    public void GetTimetablesInRoom_GetFromEmptyRoom_ReturnsTrue()
    {
        var roomId = Guid.NewGuid();
        _tracker.AddRoom(roomId);

        var result = _tracker.GetTimetablesInRoom(roomId, out var timetables);

        result.ShouldBeTrue();
        timetables.ShouldBe([]);
    }

    [Fact]
    public void GetTimetablesInRoom_GetFromExistingRoom_ReturnsCorrectList()
    {
        var roomId = Guid.NewGuid();
        var timetable = MakeTimetable(roomId);

        _tracker.AddRoom(roomId);
        _tracker.AddOrUpdateTimetable(timetable);

        var result = _tracker.GetTimetablesInRoom(roomId, out var timetables);

        result.ShouldBeTrue();
        timetables.ShouldBe([timetable]);
    }

    // === GetUsersInRoom ===

    [Fact]
    public void GetUsersInRoom_GetNonExistingRoom_ReturnsFalse()
    {
        var result = _tracker.GetUsersInRoom(Guid.NewGuid(), out _);

        result.ShouldBeFalse();
    }

    [Fact]
    public void GetUsersInRoom_GetFromEmptyRoom_ReturnsTrue()
    {
        var roomId = Guid.NewGuid();
        _tracker.AddRoom(roomId);

        var result = _tracker.GetUsersInRoom(roomId, out var timetables);

        result.ShouldBeTrue();
        timetables.ShouldBe([]);
    }

    [Fact]
    public void GetUsersInRoom_GetFromExistingRoom_ReturnsCorrectList()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        _tracker.AddUserToRoom(userId, roomId);

        var result = _tracker.GetUsersInRoom(roomId, out var timetables);

        result.ShouldBeTrue();
        timetables.ShouldBe([userId]);
    }

    // === RemoveUserFromRoom ===

    [Fact]
    public void RemoveUserFromRoom_RemoveFromEmptyRoom_ReturnsTrue()
    {
        var roomId = Guid.NewGuid();
        _tracker.AddRoom(roomId);

        var result = _tracker.RemoveUserFromRoom(Guid.NewGuid(), roomId);

        result.ShouldBeTrue();
    }

    [Fact]
    public void RemoveUserFromRoom_RemoveFromNonExistingRoom_ReturnsFalse()
    {
        var result = _tracker.RemoveUserFromRoom(Guid.NewGuid(), Guid.NewGuid());

        result.ShouldBeFalse();
    }

    [Fact]
    public void RemoveUserFromRoom_RemoveFromExistingRoom_RemovesUserFromRoom()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        _tracker.AddUserToRoom(userId, roomId);
        _tracker.AddUserToRoom(Guid.NewGuid(), roomId);

        var success = _tracker.RemoveUserFromRoom(userId, roomId);
        _tracker.GetUsersInRoom(roomId, out var users);

        users.Contains(userId).ShouldBeFalse();
        success.ShouldBeTrue();
    }

    [Fact]
    public void RemoveUserFromRoom_RemoveFromExistingRoom_EmptyRoomIsClosed()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        _tracker.AddUserToRoom(userId, roomId);

        var success = _tracker.RemoveUserFromRoom(userId, roomId);

        success.ShouldBeTrue();
        _tracker.RoomExists(roomId).ShouldBeFalse();
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

        timetable.Name = changedName;
        _tracker.AddOrUpdateTimetable(timetable);

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

        var success = _tracker.GetChangedTimetables(roomId, out var changedTimetables);

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

    // === SetRoom ===

    [Fact]
    public void SetRoom_SetRoomWithEmpty_SetsCorrectly()
    {
        var roomId = Guid.NewGuid();

        _tracker.SetRoom(roomId, [], []);

        _tracker.GetTimetablesInRoom(roomId, out var timetables).ShouldBeTrue();
        _tracker.GetUsersInRoom(roomId, out var users).ShouldBeTrue();

        timetables.ShouldBeEmpty();
        users.ShouldBeEmpty();
    }

    [Fact]
    public void SetRoom_SetRoomWithFilledLists_SetsCorrectly()
    {
        var roomId = Guid.NewGuid();

        IReadOnlyCollection<Guid> users = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        IReadOnlyCollection<Timetable> timetables = [MakeTimetable(roomId), MakeTimetable(roomId)];

        _tracker.SetRoom(roomId, users, timetables);

        _tracker.GetTimetablesInRoom(roomId, out var timetablesRes).ShouldBeTrue();
        _tracker.GetUsersInRoom(roomId, out var usersRes).ShouldBeTrue();

        timetablesRes.ShouldBe(timetables, ignoreOrder: true);
        usersRes.ShouldBe(users, ignoreOrder: true);
    }

    [Fact]
    public void SetRoom_SetRoomOnExistingRoom_ReturnsFalse()
    {
        var roomId = Guid.NewGuid();

        _tracker.SetRoom(roomId, [], []);
        var res = _tracker.SetRoom(roomId, [], []);

        res.ShouldBeFalse();
    }

    [Fact]
    public void SetRoom_SetRoomWithWrongTimetableRoomId_ThrowsArgumentException()
    {
        var roomId = Guid.NewGuid();

        IReadOnlyCollection<Guid> users = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        IReadOnlyCollection<Timetable> timetables =
        [
            MakeTimetable(roomId),
            MakeTimetable(Guid.NewGuid()),
        ];

        Action act = () => _tracker.SetRoom(roomId, users, timetables);

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
    public void CloseRoom_CloseExistingRoom_RemovesExistingUserAndTimetablesFromCache()
    {
        var roomId = Guid.NewGuid();

        IReadOnlyCollection<Guid> users = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        IReadOnlyCollection<Timetable> timetables = [MakeTimetable(roomId), MakeTimetable(roomId)];

        _tracker.SetRoom(roomId, users, timetables);
        _tracker.CloseRoom(roomId);

        _tracker.GetTimetablesInRoom(roomId, out _).ShouldBeFalse();
        _tracker.GetUsersInRoom(roomId, out _).ShouldBeFalse();

        users.ToList().ForEach(id => _tracker.GetRoomOfUser(id, out _).ShouldBeFalse());
    }

    [Fact]
    public void CloseRoom_CloseExistingChangedRoom_RemovesChangedTimetablesFromCache()
    {
        var roomId = Guid.NewGuid();

        IReadOnlyCollection<Guid> users = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        List<Timetable> timetables = [MakeTimetable(roomId), MakeTimetable(roomId)];

        timetables[0].Name = "Changed!";

        _tracker.SetRoom(roomId, users, timetables);
        _tracker.AddOrUpdateTimetable(timetables[0]);

        _tracker.CloseRoom(roomId);

        _tracker.GetChangedTimetables(roomId, out _).ShouldBeFalse();
    }

    // === GetChangedTimetables ===

    [Fact]
    public void GetChangedTimetables_GetOnNonExistingRoom_ReturnsFalse()
    {
        var result = _tracker.GetChangedTimetables(Guid.NewGuid(), out _);

        result.ShouldBeFalse();
    }

    [Fact]
    public void GetChangedTimetables_GetOnExistingRoom_ReturnsEmpty()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);
        _tracker.GetChangedTimetables(roomId, out var res).ShouldBeTrue();

        res.ShouldBeEmpty();
    }

    [Fact]
    public void GetChangedTimetables_GetOnExistingRoom_ReturnsCorrectChanged()
    {
        var roomId = Guid.NewGuid();

        _tracker.AddRoom(roomId);

        IReadOnlyCollection<Guid> users = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        List<Timetable> timetables = [MakeTimetable(roomId), MakeTimetable(roomId)];

        _tracker.SetRoom(roomId, users, timetables);

        var changedTimetable = timetables[0];
        changedTimetable.Name = "Changed!";
        _tracker.AddOrUpdateTimetable(changedTimetable);

        _tracker.GetChangedTimetables(roomId, out var res).ShouldBeTrue();
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
    public void RemoveTimetablesFromChanged_RemoveFilledFromEmptyList_ReturnsOriginal()
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

        IReadOnlyCollection<Timetable> timetables = [MakeTimetable(roomId), MakeTimetable(roomId)];

        _tracker.SetRoom(roomId, [], timetables);

        var changed = timetables.First();
        changed.Name = "This has been changed!";
        _tracker.AddOrUpdateTimetable(changed);

        var secondChanged = timetables.ElementAt(1);
        secondChanged.Name = "This has also been changed!";
        _tracker.AddOrUpdateTimetable(secondChanged);

        var res = _tracker.RemoveTimetablesFromChanged([changed.Id]);
        res.ShouldBeEmpty();

        _tracker.GetChangedTimetables(roomId, out var changedRes).ShouldBeTrue();
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

        IReadOnlyCollection<Timetable> timetables = [MakeTimetable(roomId), MakeTimetable(roomId)];

        _tracker.SetRoom(roomId, [], timetables);
        _tracker.DeleteTimetable(roomId, timetables.ElementAt(0).Id);
        _tracker.DeleteTimetable(roomId, timetables.ElementAt(1).Id);

        var res = _tracker.RemoveTimetablesFromChanged([timetables.ElementAt(0).Id]);
        res.ShouldBeEmpty();

        _tracker.RemoveTimetablesFromDeleted(roomId, [timetables.ElementAt(0).Id]);

        var deletedRes = _tracker.GetDeletedTimetables(roomId);
        deletedRes.ShouldBe([timetables.ElementAt(1).Id]);
    }

    private static Timetable MakeTimetable(Guid? roomId = null, string name = "Test") =>
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
}
