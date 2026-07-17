using Backend.DTOs;
using Backend.Models;

namespace Backend.Services.Rooms;

public interface IRoomService
{
    public bool RoomExists(Guid roomId);

    public Task RegisterConnectionAsync(Guid userId, string connectionId);
    public Task<RoomConnectionMove> CreateOrJoinRoom(
        Guid roomId,
        Guid userId,
        string connectionId
    );
    public Task<RoomConnectionDeparture?> HandleLeaveRoom(
        Guid roomId,
        string connectionId
    );
    public Task<ConnectionRemoval?> HandleDisconnectAsync(string connectionId);
    public bool GetRoomOfConnection(string connectionId, out Guid roomId);
    public bool IsConnectionInRoom(string connectionId, Guid roomId);
    public bool HandleDeleteTimetable(Guid roomId, Guid userId, Guid timetableId);

    public CreateTimetableResult HandleCreateTimetable(
        Guid roomId,
        Guid userId,
        CreateTimetableRequest timetableRequest,
        Guid? CopyOf
    );

    // TODO: Maybe add in some auth feature so that some people can "lock" their timetable
    public Task<bool> HandleUpdateTimetableAsync(
        Guid roomId,
        Guid userId,
        Guid timetableId,
        UpdateTimetableRequest timetableRequest
    );

    public Task<RoomInformation?> GetRoomInformationAsync(Guid roomId, Guid userId);

    public Task<IReadOnlyCollection<UserSearchResponse>> FindUsersByHandle(
        string handle,
        Guid roomId,
        Guid callerId
    );
    public Task<IReadOnlyCollection<RoomMemberResponse>?> GetRoomMembersAsync(
        Guid roomId,
        Guid userId
    );
    public Task<IReadOnlyCollection<TimetableDetailedResponse>?> GetTimetablesDetailedInRoomAsync(
        Guid roomId,
        Guid userId
    );

    public Task<bool> CloseRoom(Guid roomId);
    public Task<bool> CommitChangesAsync(Guid roomId);

    public Task SetMemberRole(Guid roomId, Guid userId, RoomRole role, Guid callerId);
    public Task<IReadOnlyCollection<string>> RevokeMemberAccess(
        Guid roomId,
        Guid userId,
        Guid callerId
    );
}
