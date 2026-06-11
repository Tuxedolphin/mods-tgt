namespace Backend.Services.Room;

public interface IRoomService
{
    public void CreateRoom(Guid timetableId);
    public bool HandleJoinRoom(Guid userId, Guid roomId);
    public bool HandleLeaveRoom(Guid userId, Guid roomId);
}
