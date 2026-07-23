using Backend.DTOs;
using Backend.Exceptions;
using Backend.Hubs.Clients;
using Backend.Infrastructure;
using Backend.Models;
using Backend.Services.Rooms;
using Backend.Services.Timetables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

[Authorize]
public class RoomHub(
    ILogger<RoomHub> logger,
    IRoomService roomService,
    ITimetableService timetableService
) : Hub<IRoomHubClient>
{
    private readonly ILogger<RoomHub> _logger = logger;
    private readonly IRoomService _roomService = roomService;
    private readonly ITimetableService _timetableService = timetableService;

    private Guid GetUserId()
    {
        if (Context.User == null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return ClaimsHelper.GetUserId(Context.User);
    }

    private Guid GetCurrentRoomId()
    {
        if (!_roomService.TryGetRoomOfConnection(Context.ConnectionId, out var roomId))
            throw new HubException($"Connection {Context.ConnectionId} has not joined a room.");

        return roomId;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        RoomHubLogs.LogUserConnected(_logger, userId, Context.ConnectionId);

        await _roomService.RegisterConnectionAsync(userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var removal = await _roomService.HandleDisconnectAsync(Context.ConnectionId);
            var userId = removal?.UserId ?? GetUserId();
            RoomHubLogs.LogUserDisconnected(_logger, exception, userId, Context.ConnectionId);

            if (removal?.RoomId is { } roomId)
            {
                try
                {
                    await SendUpdatedRoomMembersToGroupAsync(roomId, removal.UserId);
                }
                catch (NotFoundException)
                {
                    // The final connection leaving may close the in-memory room.
                }
            }
        }
        catch (Exception cleanUpException)
        {
            RoomHubLogs.LogDisconnectCleanUpFailed(_logger, cleanUpException, Context.ConnectionId);
        }
        finally
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    public async Task<RoomInformation> CreateOrJoinRoom(Guid roomId)
    {
        try
        {
            var userId = GetUserId();

            var move = await _roomService.CreateOrJoinRoomAsync(
                roomId,
                userId,
                Context.ConnectionId
            );

            if (move.PreviousRoomId is { } previousRoomId && previousRoomId != roomId)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, previousRoomId.ToString());

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
            }
            catch
            {
                if (move.PreviousRoomId != roomId)
                    await _roomService.HandleLeaveRoomAsync(roomId, Context.ConnectionId);

                throw;
            }

            if (!_roomService.IsConnectionInRoom(Context.ConnectionId, roomId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
                throw new HubException("Room access was revoked while joining.");
            }

            RoomHubLogs.LogUserJoinedRoom(_logger, userId, roomId);

            if (move.PreviousRoomId is { } oldRoomId && oldRoomId != roomId)
                await TrySendUpdatedRoomMembersToGroupAsync(oldRoomId);

            var roomInformation =
                await _roomService.GetRoomInformationAsync(roomId, userId)
                ?? throw new HubException("Failed to retrieve room information");

            await Clients
                .Group(roomId.ToString())
                .ReceiveRoomMembersUpdate(roomInformation.Members);

            await Clients.Caller.ReceiveTimetableUpdate(roomInformation.Timetables);

            Console.WriteLine($"Room information sent to user {userId} for room {roomId}");
            return roomInformation;
        }
        catch (NotFoundException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    public async Task LeaveRoom(Guid roomId)
    {
        var departure = await _roomService.HandleLeaveRoomAsync(roomId, Context.ConnectionId);
        if (departure is null)
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
        RoomHubLogs.LogUserLeftRoom(_logger, departure.UserId, roomId);

        await TrySendUpdatedRoomMembersToGroupAsync(roomId);
    }

    public async Task<RoomInformation> GetRoomInformation(Guid roomId)
    {
        return await _roomService.GetRoomInformationAsync(roomId, GetUserId())
            ?? throw new HubException($"Room {roomId} not found");
    }

    public async Task CreateTimetable(CreateTimetableRequest timetableRequest)
    {
        var userId = GetUserId();
        var roomId = GetCurrentRoomId();

        _roomService.HandleCreateTimetable(roomId, userId, timetableRequest, null);

        await SendUpdatedTimetableToGroupAsync(roomId);
    }

    public async Task CopyTimetableTo(Guid timetableId, Guid timetableIdToCopyTo)
    {
        try
        {
            var userId = GetUserId();
            var timetableToCopyTo = await _timetableService.GetTimetableByIdAsync(
                timetableIdToCopyTo,
                userId
            );
            var timetable = await _timetableService
                .GetTimetableByIdAsync(timetableId, userId)
                .MapAsync(t => new UpdateTimetableRequest()
                {
                    Name = timetableToCopyTo.Name,
                    MetaData = t.MetaData,
                });

            await _roomService.HandleUpdateTimetableAsync(
                GetCurrentRoomId(),
                userId,
                timetableIdToCopyTo,
                timetable
            );

            await SendUpdatedTimetableToGroupAsync(GetCurrentRoomId());
        }
        catch (NotFoundException)
        {
            throw new HubException($"Timetable with id {timetableId} not found");
        }
    }

    public async Task UpdateRoomVisibility(Guid roomId, Visibility visibility)
    {
        try
        {
            var removedConnectionIds = await _roomService.UpdateRoomVisibilityAsync(
                roomId,
                GetUserId(),
                visibility
            );
            await Task.WhenAll(
                removedConnectionIds.Select(connectionId =>
                    Groups.RemoveFromGroupAsync(connectionId, roomId.ToString())
                )
            );
            await SendUpdatedVisibilityToGroupAsync(roomId, visibility);
        }
        catch (NotFoundException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    // Returns null when timetable was not found
    public async Task<bool> UpdateTimetable(
        Guid timetableId,
        UpdateTimetableRequest timetableRequest
    )
    {
        var userId = GetUserId();
        var roomId = GetCurrentRoomId();

        var success = await _roomService.HandleUpdateTimetableAsync(
            roomId,
            userId,
            timetableId,
            timetableRequest
        );
        await SendUpdatedTimetableToGroupAsync(roomId);

        return success;
    }

    public async Task DeleteTimetable(Guid timetableId)
    {
        var userId = GetUserId();
        var roomId = GetCurrentRoomId();

        if (!_roomService.HandleDeleteTimetable(roomId, userId, timetableId))
        {
            throw new HubException(
                "Cannot delete main timetable of the room in Hub. Please use DEL /timetable/{id}"
            );
        }

        await SendUpdatedTimetableToGroupAsync(roomId);
    }

    public async Task<IReadOnlyCollection<UserSearchResponse>> FindUsersByHandle(
        string handle,
        Guid roomId
    )
    {
        return await _roomService.FindUsersByHandleAsync(handle, roomId, GetUserId());
    }

    public async Task SetMemberRole(Guid userId, Guid roomId, RoomRole role)
    {
        var callerId = GetUserId();

        await _roomService.SetMemberRoleAsync(roomId, userId, role, callerId);
        await SendUpdatedRoomMembersToGroupAsync(roomId);
    }

    public async Task RevokeMemberAccess(Guid userId, Guid roomId)
    {
        var callerId = GetUserId();

        var removedConnectionIds = await _roomService.RevokeMemberAccessAsync(
            roomId,
            userId,
            callerId
        );
        await Task.WhenAll(
            removedConnectionIds.Select(connectionId =>
                Groups.RemoveFromGroupAsync(connectionId, roomId.ToString())
            )
        );

        await SendUpdatedRoomMembersToGroupAsync(roomId);
    }

    private async Task SendUpdatedTimetableToGroupAsync(Guid roomId) =>
        await Clients
            .Group(roomId.ToString())
            .ReceiveTimetableUpdate(
                await _roomService.GetTimetablesDetailedInRoomAsync(roomId, GetUserId())
                    ?? throw new NotFoundException($"Room with id ${roomId} was not found")
            );

    private async Task SendUpdatedRoomMembersToGroupAsync(
        Guid roomId,
        Guid? requestingUserId = null
    ) =>
        await Clients
            .Group(roomId.ToString())
            .ReceiveRoomMembersUpdate(
                await _roomService.GetRoomMembersAsync(roomId, requestingUserId ?? GetUserId())
                    ?? throw new NotFoundException($"Room with id ${roomId} was not found")
            );

    private async Task TrySendUpdatedRoomMembersToGroupAsync(Guid roomId)
    {
        try
        {
            await SendUpdatedRoomMembersToGroupAsync(roomId);
        }
        catch (NotFoundException)
        {
            // The room may close after its final connection leaves.
        }
    }

    private async Task SendUpdatedVisibilityToGroupAsync(Guid roomId, Visibility visibility)
    {
        await Clients.Group(roomId.ToString()).ReceiveRoomVisibilityUpdate(visibility);
    }

    // This method is used mainly as a placeholder for testing, but it could be used in the future
    // to send a message as a chat feature
    public async Task SendMessageToRoom(string message)
    {
        Guid userId = GetUserId();
        var roomId = GetCurrentRoomId();

        // roomId is string here, no nullable issues
        await Clients
            .Group(roomId.ToString())
            .ReceiveMessage(new MessageResponse(userId, message, DateTime.Now));
    }
}
