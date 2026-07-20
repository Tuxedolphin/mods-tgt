namespace Backend.Hubs;

public static partial class RoomHubLogs
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "User {UserId} connected to RoomHub with ConnectionId {ConnectionId}"
    )]
    public static partial void LogUserConnected(ILogger logger, Guid userId, string connectionId);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        Message = "User {UserId} disconnected from RoomHub with ConnectionId {ConnectionId}."
    )]
    public static partial void LogUserDisconnected(
        ILogger logger,
        Exception? exception,
        Guid userId,
        string connectionId
    );

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Debug,
        Message = "User {UserId} joined room {RoomId} in RoomHub."
    )]
    public static partial void LogUserJoinedRoom(ILogger logger, Guid userId, Guid roomId);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Debug,
        Message = "User {UserId} left room {RoomId} in RoomHub."
    )]
    public static partial void LogUserLeftRoom(ILogger<RoomHub> logger, Guid userId, Guid roomId);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Warning,
        Message = "Failed to clean up RoomHub connection {ConnectionId} during disconnect."
    )]
    public static partial void LogDisconnectCleanUpFailed(
        ILogger logger,
        Exception exception,
        string connectionId
    );
}
