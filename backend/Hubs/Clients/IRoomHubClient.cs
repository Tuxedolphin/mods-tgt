using Backend.DTOs;

namespace Backend.Hubs.Clients;

public interface IRoomHubClient
{
    Task ReceiveMessage(MessageResponse response);

    Task ReceiveTimetableUpdate(IReadOnlyCollection<TimetableDetailedResponse> timetables);
    Task ReceiveUserUpdate(IReadOnlyCollection<ProfileResponse> profiles);
}
