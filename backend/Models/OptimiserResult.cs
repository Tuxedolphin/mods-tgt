namespace Backend.Models;

public class OptimiserResult
{
    public Guid Id { get; set; }

    public required Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    // Null for the room's shared group result, set for a user's own solo result
    public Guid? UserId { get; set; }

    public required Guid RequestedBy { get; set; }
    public required Guid SolveId { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string PayloadJson { get; set; }
}
