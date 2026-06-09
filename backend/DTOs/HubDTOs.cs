namespace Backend.DTOs;

public record RoomInformation(Guid RoomId, IReadOnlyCollection<Guid> Users);

public record MessageResponse(Guid UserId, string Content, DateTime SentAt);
