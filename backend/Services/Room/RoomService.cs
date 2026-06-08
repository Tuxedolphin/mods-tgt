using Backend.DTOs;

namespace Backend.Services.Room;

public class RoomService(ILogger<RoomService> logger, IRoomTracker roomTracker) : IRoomService
{
    private readonly ILogger<RoomService> _logger = logger;
    private readonly IRoomTracker _roomTracker = roomTracker;

    public Guid CreateRoom()
    {
        Guid newGuid = Guid.NewGuid();

        // We don't check for duplicate here since the changes of Guid collision is astronomically low
        _roomTracker.AddRoom(newGuid);

        return newGuid;
    }

    public bool HandleJoinRoom(Guid userId, Guid roomId)
    {
        if (_roomTracker.GetRoomOfUser(userId, out Guid oldRoomId))
            HandleLeaveRoom(userId, oldRoomId);

        if (_roomTracker.AddUserToRoom(userId, roomId))
            return true;

        RoomServiceLogs.LogAttemptedJoinNonExistentRoom(_logger, roomId, userId);
        return false;
    }

    public bool HandleLeaveRoom(Guid userId, Guid roomId)
    {
        if (_roomTracker.RemoveUserFromRoom(userId, roomId))
            return true;

        RoomServiceLogs.LogAttemptedLeaveNonExistentRoom(_logger, roomId, userId);
        return false;
    }
}
