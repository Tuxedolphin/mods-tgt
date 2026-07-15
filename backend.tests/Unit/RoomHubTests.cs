using System.Security.Claims;
using Backend.DTOs;
using Backend.Hubs;
using Backend.Hubs.Clients;
using Backend.Services.Profiles;
using Backend.Services.Rooms;
using Backend.Services.Timetables;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Backend.Tests.Unit;

public class RoomHubTests
{
    [Fact]
    public async Task SetMemberRole_BroadcastsUpdatedRoomMembers()
    {
        var callerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var updatedMembers = new List<RoomMemberResponse>();
        var (hub, service, roomClient, _) = CreateHub(callerId, roomId, updatedMembers);

        await hub.SetMemberRole(memberId, roomId, RoomRole.Viewer);

        await service.Received(1).SetMemberRole(roomId, memberId, RoomRole.Viewer, callerId);
        await roomClient.Received(1).ReceiveRoomMembersUpdate(updatedMembers);
    }

    [Fact]
    public async Task RevokeMemberAccess_RemovesConnectionsFromGroupAndBroadcastsUpdatedMembers()
    {
        var callerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var updatedMembers = new List<RoomMemberResponse>();
        IReadOnlyCollection<string> removedConnectionIds =
            ["member-connection-1", "member-connection-2"];
        var (hub, service, roomClient, groups) = CreateHub(callerId, roomId, updatedMembers);
        service
            .RevokeMemberAccess(roomId, memberId, callerId)
            .Returns(removedConnectionIds);

        await hub.RevokeMemberAccess(memberId, roomId);

        await service.Received(1).RevokeMemberAccess(roomId, memberId, callerId);
        foreach (var connectionId in removedConnectionIds)
        {
            await groups
                .Received(1)
                .RemoveFromGroupAsync(connectionId, roomId.ToString());
        }
        await roomClient.Received(1).ReceiveRoomMembersUpdate(updatedMembers);
    }

    private static (
        RoomHub Hub,
        IRoomService Service,
        IRoomHubClient RoomClient,
        IGroupManager Groups
    ) CreateHub(
        Guid callerId,
        Guid roomId,
        IReadOnlyCollection<RoomMemberResponse> updatedMembers
    )
    {
        var service = Substitute.For<IRoomService>();
        service.GetRoomMembersAsync(roomId, callerId).Returns(updatedMembers);

        var hub = new RoomHub(
            NullLogger<RoomHub>.Instance,
            service,
            Substitute.For<ITimetableService>()
        );
        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("caller-connection");
        context.User.Returns(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, callerId.ToString())],
                    "test"
                )
            )
        );
        hub.Context = context;
        var groups = Substitute.For<IGroupManager>();
        hub.Groups = groups;

        var clients = Substitute.For<IHubCallerClients<IRoomHubClient>>();
        var roomClient = Substitute.For<IRoomHubClient>();
        clients.Group(roomId.ToString()).Returns(roomClient);
        hub.Clients = clients;

        return (hub, service, roomClient, groups);
    }
}
