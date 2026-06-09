using Backend.DTOs;

namespace Backend.Services.Room;

public interface IRoomTracker
{
    public bool AddRoom(Guid roomId);
    public bool AddUserToRoom(Guid userId, Guid roomId);
    public bool RemoveUserFromRoom(Guid userId, Guid roomId);
    public bool GetRoomOfUser(Guid userId, out Guid rooomId);
    public IReadOnlyCollection<Guid> GetUsersInRoom(Guid roomId);
    public IReadOnlyCollection<RoomInformation> GetAllRoomInformation();
}
