using Backend.Models;

namespace Backend.Services.Rooms;

public interface IRoomTracker
{
    public bool AddRoom(Guid roomId);
    public bool RoomExists(Guid roomId);
    public bool SetRoom(
        Guid roomId,
        IReadOnlyCollection<Guid> users,
        IReadOnlyCollection<Timetable> timetables
    );

    public bool AddUserToRoom(Guid userId, Guid roomId);
    public bool RemoveUserFromRoom(Guid userId, Guid roomId);

    public bool GetRoomOfUser(Guid userId, out Guid rooomId);
    public bool GetUsersInRoom(Guid roomId, out IReadOnlyCollection<Guid> users);

    public bool GetTimetablesInRoom(Guid roomId, out IReadOnlyCollection<Timetable> timetables);
    public Timetable? GetTimetableById(Guid roomId, Guid timetableId);
    public bool AddOrUpdateTimetable(Timetable timetable);
    public bool DeleteTimetable(Guid roomId, Guid timetableId);

    public bool CloseRoom(Guid roomId);

    public bool GetChangedTimetables(Guid roomId, out IReadOnlyCollection<Timetable> timetables);
    public IReadOnlyCollection<Guid> GetDeletedTimetables(Guid roomId);
    public IReadOnlyCollection<Guid> RemoveTimetablesFromChanged(
        IReadOnlyCollection<Guid> timetables
    );
    public IReadOnlyCollection<Guid> RemoveTimetablesFromDeleted(
        Guid roomId,
        IReadOnlyCollection<Guid> timetables
    );
}
