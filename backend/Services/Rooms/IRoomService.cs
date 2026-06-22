using Backend.DTOs;
using Backend.Models;

namespace Backend.Services.Rooms;

public interface IRoomService
{
    public void CreateRoom(Guid roomId);
    public bool RoomExists(Guid roomId);

    public bool HandleJoinRoom(Guid userId, Guid roomId);
    public bool HandleLeaveRoom(Guid userId, Guid roomId);
    public bool HandleDeleteTimetable(Guid roomId, Guid timetableId);

    public CreateTimetableResult HandleCreateTimetable(
        Guid roomId,
        Guid userId,
        CreateTimetableRequest timetableRequest,
        Guid? CopyOf
    );

    // We do not check for the userId here for auth since all users in the room should be able to update any of the timetables
    // TODO: Maybe add in some auth feature so that some people can "lock" their timetable
    public Task<bool> HandleUpdateTimetable(
        Guid roomId,
        Guid timetableId,
        UpdateTimetableRequest timetableRequest
    );
    public Task<RoomInformation?> GetRoomInformation(Guid roomId);

    public bool CloseRoom(Guid roomId);
    public Task<bool> CommitChanges(Guid roomId);
}
