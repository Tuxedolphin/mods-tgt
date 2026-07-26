namespace Backend.Services.Optimiser;

public static partial class OptimiserLogs
{
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Error,
        Message = "Optimiser solve failed for room {RoomId} requested by user {UserId}"
    )]
    public static partial void LogSolveFailed(
        ILogger logger,
        Exception exception,
        Guid roomId,
        Guid userId
    );
}
