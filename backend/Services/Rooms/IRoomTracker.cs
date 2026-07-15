using Backend.DTOs;

namespace Backend.Services.Rooms;

public record RoomInit(
    IReadOnlyCollection<Guid> Editors,
    IReadOnlyCollection<Guid> Viewers,
    IReadOnlyCollection<RoomTimetable> Timetables
);

public record RoomConnectionMove(Guid? PreviousRoomId);

public record RoomConnectionDeparture(Guid UserId);

public record ConnectionRemoval(
    Guid UserId,
    Guid? RoomId,
    bool WasLastConnectionForUser
);

public interface IRoomTracker
{
    public bool AddRoom(Guid roomId);
    public bool RoomExists(Guid roomId);
    public bool SetRoom(Guid roomId, RoomInit init);

    public bool RegisterConnection(string connectionId, Guid userId);
    public RoomConnectionMove MoveConnectionToRoom(
        string connectionId,
        Guid userId,
        Guid roomId
    );
    public RoomConnectionDeparture? LeaveConnectionFromRoom(
        string connectionId,
        Guid roomId
    );
    public ConnectionRemoval? RemoveConnection(string connectionId);
    public bool GetRoomOfConnection(string connectionId, out Guid roomId);
    public bool IsConnectionInRoom(string connectionId, Guid roomId);
    public bool GetUsersInRoom(Guid roomId, out IReadOnlyCollection<Guid> users);

    public bool SetMemberRole(Guid roomId, Guid userId, RoomRole role);
    public IReadOnlyCollection<string> RemoveMemberRoleAndConnections(
        Guid roomId,
        Guid userId
    );
    public bool GetEditorsInRoom(Guid roomId, out IReadOnlyCollection<Guid> editors);
    public bool GetViewersInRoom(Guid roomId, out IReadOnlyCollection<Guid> viewers);

    public bool GetTimetablesInRoom(Guid roomId, out IReadOnlyCollection<RoomTimetable> timetables);
    public RoomTimetable? GetTimetableById(Guid roomId, Guid timetableId);
    public bool AddOrUpdateTimetable(RoomTimetable timetable);
    public bool DeleteTimetable(Guid roomId, Guid timetableId);

    public bool CloseRoom(Guid roomId);

    public bool GetChangedTimetables(
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
