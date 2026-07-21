using Backend.Models;

namespace Backend.DTOs;

public record RoomInformation(
    Guid RoomId,
    IReadOnlyCollection<RoomMemberResponse> Members,
    IReadOnlyCollection<TimetableDetailedResponse> Timetables,
    Visibility Visibility
);

public record MessageResponse(Guid UserId, string Content, DateTime SentAt);

public record RoomMemberResponse(
    Guid UserId,
    string Username,
    string Handle,
    string? AvatarUrl,
    RoomRole Role,
    bool IsInRoom
);

public record UserSearchResponse(Guid UserId, string Username, string Handle, string? AvatarUrl);

public enum RoomRole
{
    Owner,
    Editor,
    Viewer,
}
