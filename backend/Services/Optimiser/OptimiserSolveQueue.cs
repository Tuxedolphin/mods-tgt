using System.Collections.Concurrent;
using System.Threading.Channels;
using Backend.DTOs;

namespace Backend.Services.Optimiser;

public record OptimiserSolveJob(
    Guid RoomId,
    Guid CallerId,
    SolveRequest? GroupRequest,
    SoloSolveRequest? SoloRequest
);

public interface IOptimiserSolveQueue
{
    /// Returns false when the caller already has a solve in flight for this room.
    bool TryEnqueue(OptimiserSolveJob job);

    IAsyncEnumerable<OptimiserSolveJob> ReadAllAsync(CancellationToken ct);

    void Complete(OptimiserSolveJob job);
}

public class OptimiserSolveQueue : IOptimiserSolveQueue
{
    private readonly Channel<OptimiserSolveJob> _jobs =
        Channel.CreateUnbounded<OptimiserSolveJob>();

    private readonly ConcurrentDictionary<(Guid RoomId, Guid CallerId), byte> _inFlight = new();

    public bool TryEnqueue(OptimiserSolveJob job)
    {
        if (!_inFlight.TryAdd((job.RoomId, job.CallerId), 0))
            return false;

        if (_jobs.Writer.TryWrite(job))
            return true;

        _inFlight.TryRemove((job.RoomId, job.CallerId), out _);
        return false;
    }

    public IAsyncEnumerable<OptimiserSolveJob> ReadAllAsync(CancellationToken ct) =>
        _jobs.Reader.ReadAllAsync(ct);

    public void Complete(OptimiserSolveJob job) =>
        _inFlight.TryRemove((job.RoomId, job.CallerId), out _);
}
