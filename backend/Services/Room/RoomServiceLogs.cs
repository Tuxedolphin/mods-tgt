namespace Backend.Services.Room;

public static partial class RoomServiceLogs
{
    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Warning,
        Message = "Attempted to join non-existent room {RoomId} by user {UserId}"
    )]
    public static partial void LogAttemptedJoinNonExistentRoom(
        ILogger logger,
        Guid roomId,
        Guid userId
    );

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Warning,
        Message = "Attempted to leave non-existent room {RoomId} by user {UserId}"
    )]
    public static partial void LogAttemptedLeaveNonExistentRoom(
        ILogger logger,
        Guid roomId,
        Guid userId
    );
}
