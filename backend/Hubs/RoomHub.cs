using Backend.DTOs;
using Backend.Hubs.Clients;
using Backend.Infrastructure;
using Backend.Models;
using Backend.Services.Room;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

[Authorize]
public class RoomHub(ILogger<RoomHub> logger, IRoomService roomService, IRoomTracker roomTracker)
    : Hub<IRoomHubClient>
{
    private readonly ILogger<RoomHub> _logger = logger;
    private readonly IRoomService _roomService = roomService;
    private readonly IRoomTracker _roomTracker = roomTracker;

    private Guid GetUserId()
    {
        if (Context.User == null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return ClaimsHelper.GetUserId(Context.User);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        RoomHubLogs.LogUserConnected(_logger, userId, Context.ConnectionId);

        // TODO: some sort of timer (with below) which automatically reconnects a user back to a room

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        RoomHubLogs.LogUserDisconnected(_logger, exception, userId, Context.ConnectionId);

        // TODO: some sort of timer which when expires logs the user out of the room as well

        await base.OnDisconnectedAsync(exception);
    }

    public RoomInformation CreateRoom(Guid timetableId)
    {
        _roomService.CreateRoom(timetableId);
        return new RoomInformation(timetableId, []);
    }

    public async Task JoinRoom(Guid roomId)
    {
        // TODO: Check if the user is allowed to join the room before adding them to the group

        var userId = GetUserId();

        if (!_roomService.HandleJoinRoom(userId, roomId))
            throw new HubException($"Room with roomId {roomId} not found");

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
        RoomHubLogs.LogUserJoinedRoom(_logger, userId, roomId);
    }

    public async Task LeaveRoom(Guid roomId)
    {
        var userId = GetUserId();
        _roomService.HandleLeaveRoom(userId, roomId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
        RoomHubLogs.LogUserLeftRoom(_logger, userId, roomId);
    }

    public IReadOnlyCollection<RoomInformation> GetAllRoomInformation()
    {
        return _roomTracker.GetAllRoomInformation();
    }

    public async Task updateTimetable(Guid timetableId, Timetable timetable) { }

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
