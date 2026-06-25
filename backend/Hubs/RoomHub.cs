using Backend.DTOs;
using Backend.Exceptions;
using Backend.Hubs.Clients;
using Backend.Infrastructure;
using Backend.Services.Rooms;
using Backend.Services.Timetables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

[Authorize]
public class RoomHub(
    ILogger<RoomHub> logger,
    IRoomService roomService,
    ITimetableService timetableService,
    IRoomTracker roomTracker,
    IProfileTracker profileTracker
) : Hub<IRoomHubClient>
{
    private readonly ILogger<RoomHub> _logger = logger;
    private readonly IRoomService _roomService = roomService;
    private readonly ITimetableService _timetableService = timetableService;
    private readonly IRoomTracker _roomTracker = roomTracker;
    private readonly IProfileTracker _profileTracker = profileTracker;

    private Guid GetUserId()
    {
        if (Context.User == null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return ClaimsHelper.GetUserId(Context.User);
    }

    private Guid GetCurrentRoomId(Guid userId)
    {
        if (!_roomTracker.GetRoomOfUser(userId, out var roomId))
            throw new HubException($"User {userId} has not joined any rooms.");

        return roomId;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        RoomHubLogs.LogUserConnected(_logger, userId, Context.ConnectionId);

        await _roomService.AddProfileAsync(userId);

        // TODO: some sort of timer (with below) which automatically reconnects a user back to a room

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        RoomHubLogs.LogUserDisconnected(_logger, exception, userId, Context.ConnectionId);

        await LeaveRoom(GetCurrentRoomId(userId));
        _profileTracker.RemoveUser(userId);

        // TODO: some sort of timer which when expires logs the user out of the room as well

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<RoomInformation> CreateOrJoinRoom(Guid roomId)
    {
        // TODO: Check if the user is allowed to join the room before adding them to the group

        var userId = GetUserId();
        _roomService.CreateOrJoinRoom(userId, roomId);

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
        RoomHubLogs.LogUserJoinedRoom(_logger, userId, roomId);

        var roomInformation =
            await _roomService.GetRoomInformationAsync(roomId)
            ?? throw new HubException("Failed to retrieve room information");

        await Clients.OthersInGroup(roomId.ToString()).ReceiveUserUpdate(roomInformation.Users);

        return roomInformation;
    }

    public async Task LeaveRoom(Guid roomId)
    {
        var userId = GetUserId();

        _roomService.HandleLeaveRoom(userId, roomId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
        RoomHubLogs.LogUserLeftRoom(_logger, userId, roomId);

        try
        {
            await SendUpdatedProfilesToGroupAsync(roomId);
        }
        catch (NotFoundException)
        {
            // We ignore the error here, since the room may have been deleted after the user left
            // (i.e. no user left)
        }

        await _roomService.CommitChangesAsync(roomId);
    }

    public async Task<RoomInformation> GetRoomInformation(Guid roomId)
    {
        return await _roomService.GetRoomInformationAsync(roomId)
            ?? throw new HubException($"Room {roomId} not found");
    }

    public async Task CreateTimetable(CreateTimetableRequest timetableRequest)
    {
        var userId = GetUserId();
        var roomId = GetCurrentRoomId(userId);

        _roomService.HandleCreateTimetable(roomId, userId, timetableRequest, null);

        await SendUpdatedTimetableToGroupAsync(roomId);
    }

    public async Task CreateCopyOfTimetable(Guid timetableId)
    {
        try
        {
            var userId = GetUserId();
            var timetable = await _timetableService
                .GetTimetableByIdAsync(timetableId, userId)
                .MapAsync(t => new CreateTimetableRequest()
                {
                    Name = t.Name,
                    MetaData = t.MetaData,
                });

            _roomService.HandleCreateTimetable(
                GetCurrentRoomId(userId),
                userId,
                timetable,
                timetableId
            );

            await SendUpdatedTimetableToGroupAsync(GetCurrentRoomId(userId));
        }
        catch (NotFoundException)
        {
            throw new HubException($"Timetable with id {timetableId} not found");
        }
    }

    // Returns null when timetable was not found
    public async Task<bool> UpdateTimetable(
        Guid timetableId,
        UpdateTimetableRequest timetableRequest
    )
    {
        var roomId = GetCurrentRoomId(GetUserId());

        var success = await _roomService.HandleUpdateTimetableAsync(
            roomId,
            timetableId,
            timetableRequest
        );
        await SendUpdatedTimetableToGroupAsync(roomId);

        return success;
    }

    public async Task DeleteTimetable(Guid timetableId)
    {
        var roomId = GetCurrentRoomId(GetUserId());

        if (!_roomService.HandleDeleteTimetable(roomId, timetableId))
        {
            throw new HubException(
                "Cannot delete main timetable of the room in Hub. Please use DEL /timetable/{id}"
            );
        }

        await SendUpdatedTimetableToGroupAsync(roomId);
    }

    private async Task SendUpdatedTimetableToGroupAsync(Guid roomId) =>
        await Clients
            .Group(roomId.ToString())
            .ReceiveTimetableUpdate(
                await _roomService.GetTimetablesDetailedInRoomAsync(roomId)
                    ?? throw new NotFoundException($"Room with id ${roomId} was not found")
            );

    private async Task SendUpdatedProfilesToGroupAsync(Guid roomId) =>
        await Clients
            .Group(roomId.ToString())
            .ReceiveUserUpdate(
                await _roomService.GetProfilesInRoomAsync(roomId)
                    ?? throw new NotFoundException($"Room with id ${roomId} was not found")
            );

    // This method is used mainly as a placeholder for testing, but it could be used in the future
    // to send a message as a chat feature
    public async Task SendMessageToRoom(string message)
    {
        Guid userId = GetUserId();

        if (!_roomTracker.GetRoomOfUser(userId, out var roomId))
            throw new HubException($"User {userId} has not joined any rooms.");

        // roomId is string here, no nullable issues
        await Clients
            .Group(roomId.ToString())
            .ReceiveMessage(new MessageResponse(userId, message, DateTime.Now));
    }
}
