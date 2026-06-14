using Backend.Models;

namespace Backend.Services.Room;

public interface IRoomTracker
{
    public bool AddRoom(Guid roomId);

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
    public bool UpdateTimetable(Guid roomId, Timetable timetable);
    public bool DeleteRoom(Guid roomId);
}
