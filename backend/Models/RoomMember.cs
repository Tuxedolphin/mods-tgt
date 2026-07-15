using Backend.DTOs;

namespace Backend.Models;

public class RoomMember
{
    public required Guid RoomId { get; set; }
    public required Guid UserId { get; set; }

    public required RoomRole Role { get; set; }
    public Room Room { get; set; } = null!;
}
