namespace Backend.Services.Room;

public interface IRoomService
{
    public Guid CreateRoom();
    public bool HandleJoinRoom(Guid userId, Guid roomId);
    public bool HandleLeaveRoom(Guid userId, Guid roomId);
}
