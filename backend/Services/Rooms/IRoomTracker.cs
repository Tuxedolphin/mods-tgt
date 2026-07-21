using Backend.DTOs;
using Backend.Models;

namespace Backend.Services.Rooms;

public record RoomInit(
    IReadOnlyCollection<Guid> Editors,
    IReadOnlyCollection<Guid> Viewers,
    IReadOnlyCollection<RoomTimetable> Timetables,
    Visibility Visibility = Visibility.Restricted
);

public record RoomConnectionMove(Guid? PreviousRoomId);

public record RoomConnectionDeparture(Guid UserId);

public record ConnectionRemoval(Guid UserId, Guid? RoomId, bool WasLastConnectionForUser);

public interface IRoomTracker
{
    public bool AddRoom(Guid roomId);
    public bool RoomExists(Guid roomId);
    public bool SetRoom(Guid roomId, RoomInit init);

    public bool RegisterConnection(string connectionId, Guid userId);
    public RoomConnectionMove MoveConnectionToRoom(string connectionId, Guid userId, Guid roomId);
    public RoomConnectionDeparture? LeaveConnectionFromRoom(string connectionId, Guid roomId);
    public ConnectionRemoval? RemoveConnection(string connectionId);
    public bool TryGetRoomOfConnection(string connectionId, out Guid roomId);
    public bool IsConnectionInRoom(string connectionId, Guid roomId);
    public bool TryGetUsersInRoom(Guid roomId, out IReadOnlyCollection<Guid> users);

    public bool SetMemberRole(Guid roomId, Guid userId, RoomRole role);
    public IReadOnlyCollection<string> RemoveMemberRoleAndConnections(Guid roomId, Guid userId);
    public bool TryGetEditorsInRoom(Guid roomId, out IReadOnlyCollection<Guid> editors);
    public bool TryGetViewersInRoom(Guid roomId, out IReadOnlyCollection<Guid> viewers);

    public bool TryGetVisibilityOfRoom(Guid roomId, out Visibility visibility);
    public bool TryGetTimetablesInRoom(
        Guid roomId,
        out IReadOnlyCollection<RoomTimetable> timetables
    );
    public RoomTimetable? GetTimetableById(Guid roomId, Guid timetableId);

    public bool UpdateRoomVisibility(
        Guid roomId,
        Visibility visibility,
        out IReadOnlyCollection<string> removedConnectionIds
    );
    public bool AddOrUpdateTimetable(RoomTimetable timetable);
    public bool DeleteTimetable(Guid roomId, Guid timetableId);

    public bool CloseRoom(Guid roomId);

    public bool TryGetChangedTimetables(
        Guid roomId,
        out IReadOnlyCollection<RoomTimetable> timetables
    );
    public IReadOnlyCollection<Guid> GetDeletedTimetables(Guid roomId);
    public IReadOnlyCollection<Guid> RemoveTimetablesFromChanged(
        IReadOnlyCollection<Guid> timetables
    );
    public IReadOnlyCollection<Guid> RemoveTimetablesFromDeleted(
        Guid roomId,
        IReadOnlyCollection<Guid> timetables
    );
}
