namespace Backend.Services.Rooms;

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

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Error,
        Message = "Failed to commit changes for room {RoomId} due to an invalid operation"
    )]
    public static partial void LogCommitInvalidOperation(
        ILogger logger,
        Exception exception,
        Guid roomId
    );

    [LoggerMessage(
        EventId = 3006,
        Level = LogLevel.Error,
        Message = "Failed to commit changes for room {RoomId} due to a database update error"
    )]
    public static partial void LogCommitDbUpdateFailed(
        ILogger logger,
        Exception exception,
        Guid roomId
    );
}
