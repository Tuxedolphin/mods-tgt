using Backend.DTOs;

namespace Backend.Hubs.Clients;

public interface IRoomHubClient
{
    public Task RoomCreated(RoomInformation information);
    public Task ReceiveMessage(MessageResponse response);
}
