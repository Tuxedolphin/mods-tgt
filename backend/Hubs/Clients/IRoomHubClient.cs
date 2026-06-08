using Backend.DTOs;

namespace Backend.Hubs.Clients;

public interface IRoomHubClient
{
    public Task ReceiveMessage(MessageResponse response);
}
