namespace Backend.DTOs;

public record RoomInformation(
    Guid RoomId,
    IReadOnlyCollection<ProfileResponse> Users,
    IReadOnlyCollection<TimetableDetailedResponse> Timetables
);

public record MessageResponse(Guid UserId, string Content, DateTime SentAt);
