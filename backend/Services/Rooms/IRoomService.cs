using Backend.DTOs;
using Backend.Models;

namespace Backend.Services.Rooms;

public interface IRoomService
{
    public bool RoomExists(Guid roomId);

    public Task RegisterConnectionAsync(Guid userId, string connectionId);
    public Task<RoomConnectionMove> CreateOrJoinRoomAsync(
        Guid roomId,
        Guid userId,
        string connectionId
    );
    public Task<RoomConnectionDeparture?> HandleLeaveRoomAsync(Guid roomId, string connectionId);
    public Task<ConnectionRemoval?> HandleDisconnectAsync(string connectionId);
    public bool TryGetRoomOfConnection(string connectionId, out Guid roomId);
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

    public Task<IReadOnlyCollection<string>> UpdateRoomVisibilityAsync(
        Guid roomId,
        Guid callerId,
        Visibility visibility
    );
    public Task<RoomInformation?> GetRoomInformationAsync(Guid roomId, Guid userId);

    public Task<IReadOnlyCollection<UserSearchResponse>> FindUsersByHandleAsync(
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

    public bool CloseRoom(Guid roomId);
    public Task<bool> CommitChangesAsync(Guid roomId);

    public Task SetMemberRoleAsync(Guid roomId, Guid userId, RoomRole role, Guid callerId);
    public Task<IReadOnlyCollection<string>> RevokeMemberAccessAsync(
        Guid roomId,
        Guid userId,
        Guid callerId
    );
}
