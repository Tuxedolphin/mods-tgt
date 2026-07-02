using Backend.DTOs;

namespace Backend.Services.Rooms;

public interface IRoomService
{
    public bool RoomExists(Guid roomId);

    public Task<bool> AddProfileAsync(Guid userId);

    public Task CreateOrJoinRoom(Guid userId, Guid roomId);
    public Task HandleLeaveRoom(Guid userId, Guid roomId);
    public bool HandleDeleteTimetable(Guid roomId, Guid timetableId);

    public CreateTimetableResult HandleCreateTimetable(
        Guid roomId,
        Guid userId,
        CreateTimetableRequest timetableRequest,
        Guid? CopyOf
    );

    // We do not check for the userId here for auth since all users in the room should be able to update any of the timetables
    // TODO: Maybe add in some auth feature so that some people can "lock" their timetable
    public Task<bool> HandleUpdateTimetableAsync(
        Guid roomId,
        Guid timetableId,
        UpdateTimetableRequest timetableRequest
    );

    public Task<RoomInformation?> GetRoomInformationAsync(Guid roomId);
    public Task<IReadOnlyCollection<ProfileResponse>?> GetProfilesInRoomAsync(Guid roomId);
    public Task<IReadOnlyCollection<TimetableDetailedResponse>?> GetTimetablesDetailedInRoomAsync(
        Guid roomId
    );

    public Task<bool> CloseRoom(Guid roomId);
    public Task<bool> CommitChangesAsync(Guid roomId);
}
