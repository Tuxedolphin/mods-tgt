using Backend.DTOs;
using Backend.Hubs;
using Backend.Hubs.Clients;
using Backend.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Services.Optimiser;

// Solves run here rather than on the hub connection: a solve holds several
// OR-Tools threads for up to its time budget, so the caller is answered
// immediately and the result is pushed when it is ready.
public class OptimiserSolveWorker(
    IOptimiserSolveQueue queue,
    IServiceScopeFactory scopeFactory,
    IHubContext<RoomHub, IRoomHubClient> hub,
    ILogger<OptimiserSolveWorker> logger
) : BackgroundService
{
    private static readonly int MaxConcurrentSolves = Math.Max(1, Environment.ProcessorCount / 2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var throttle = new SemaphoreSlim(MaxConcurrentSolves);
        var running = new List<Task>();

        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            await throttle.WaitAsync(stoppingToken);

            running.RemoveAll(t => t.IsCompleted);
            running.Add(RunJobAsync(job, throttle, stoppingToken));
        }

        await Task.WhenAll(running);
    }

    private async Task RunJobAsync(
        OptimiserSolveJob job,
        SemaphoreSlim throttle,
        CancellationToken ct
    )
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IOptimiserService>();

            var result =
                job.GroupRequest is not null
                    ? await service.SolveGroupAsync(job.RoomId, job.CallerId, job.GroupRequest)
                    : await service.SolveSoloAsync(job.RoomId, job.CallerId, job.SoloRequest!);

            var recipients =
                job.GroupRequest is not null
                    ? await service.GetRoomAudienceAsync(job.RoomId)
                    : [job.CallerId];

            await SendResultAsync(job.RoomId, recipients, result, ct);
        }
        catch (Exception e)
        {
            OptimiserLogs.LogSolveFailed(logger, e, job.RoomId, job.CallerId);

            var message = e switch
            {
                NotFoundException or ForbiddenException or BadRequestException => e.Message,
                ExternalServiceException => "Could not reach NUSMods, please try again.",
                _ => "The optimiser could not finish, please try again.",
            };

            await hub.Clients.User(job.CallerId.ToString()).ReceiveOptimiserError(message);
        }
        finally
        {
            queue.Complete(job);
            throttle.Release();
        }
    }

    private async Task SendResultAsync(
        Guid roomId,
        IReadOnlyList<Guid> recipients,
        SolveResponse result,
        CancellationToken ct
    )
    {
        foreach (var recipient in recipients)
        {
            ct.ThrowIfCancellationRequested();

            await hub.Clients.User(recipient.ToString())
                .ReceiveOptimiserResult(roomId, OptimiserResultFilter.ForReader(result, recipient));
        }
    }
}
