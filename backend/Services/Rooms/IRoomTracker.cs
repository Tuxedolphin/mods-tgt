using Backend.Models;

namespace Backend.Services.Rooms;

public record RoomInit(
    IReadOnlyCollection<Guid> Users,
    IReadOnlyCollection<Guid> Editors,
    IReadOnlyCollection<Guid> Visitors,
    IReadOnlyCollection<RoomTimetable> Timetables
);

public interface IRoomTracker
{
    public bool AddRoom(Guid roomId);
    public bool RoomExists(Guid roomId);
    public bool SetRoom(Guid roomId, RoomInit init);

    public bool AddUserToRoom(Guid userId, Guid roomId);
    public bool RemoveUserFromRoom(Guid userId, Guid roomId);

    public bool GetRoomOfUser(Guid userId, out Guid rooomId);
    public bool GetUsersInRoom(Guid roomId, out IReadOnlyCollection<Guid> users);

    public bool SetEditors(Guid roomId, IReadOnlyCollection<Guid> editors);
    public bool GetEditorsInRoom(Guid roomId, out IReadOnlyCollection<Guid> editors);

    public bool SetVisitors(Guid roomId, IReadOnlyCollection<Guid> visitors);
    public bool GetVisitorsInRoom(Guid roomId, out IReadOnlyCollection<Guid> visitors);

    public bool GetTimetablesInRoom(Guid roomId, out IReadOnlyCollection<RoomTimetable> timetables);
    public RoomTimetable? GetTimetableById(Guid roomId, Guid timetableId);
    public bool AddOrUpdateTimetable(RoomTimetable timetable);
    public bool DeleteTimetable(Guid roomId, Guid timetableId);

    public bool CloseRoom(Guid roomId);

    public bool GetChangedTimetables(Guid roomId, out IReadOnlyCollection<RoomTimetable> timetables);
    public IReadOnlyCollection<Guid> GetDeletedTimetables(Guid roomId);
    public IReadOnlyCollection<Guid> RemoveTimetablesFromChanged(
        IReadOnlyCollection<Guid> timetables
    );
    public IReadOnlyCollection<Guid> RemoveTimetablesFromDeleted(
        Guid roomId,
        IReadOnlyCollection<Guid> timetables
    );
}
