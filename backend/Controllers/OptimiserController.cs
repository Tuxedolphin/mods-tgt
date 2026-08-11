using Backend.DTOs;
using Backend.Exceptions;
using Backend.Hubs;
using Backend.Hubs.Clients;
using Backend.Models;
using Backend.Services.Optimiser;
using Backend.Services.Rooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class OptimiserController(
    IOptimiserPreferenceService preferenceService,
    IOptimiserService optimiserService,
    IRoomMembershipChecker membershipChecker,
    IOptimiserSolveQueue solveQueue,
    IHubContext<RoomHub, IRoomHubClient> hub
) : BaseController
{
    private readonly IOptimiserPreferenceService _preferenceService = preferenceService;
    private readonly IOptimiserService _optimiserService = optimiserService;
    private readonly IRoomMembershipChecker _membershipChecker = membershipChecker;
    private readonly IOptimiserSolveQueue _solveQueue = solveQueue;
    private readonly IHubContext<RoomHub, IRoomHubClient> _hub = hub;

    [HttpGet("preferences")]
    public async Task<ActionResult<PreferencePayload>> GetGlobalPreferences()
    {
        var payload = await _preferenceService.GetAsync(GetUserId(), null);

        return payload is null ? throw new NotFoundException("No preferences set") : Ok(payload);
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> PutGlobalPreferences([FromBody] PreferencePayload payload)
    {
        await _preferenceService.UpsertAsync(GetUserId(), null, payload);

        return NoContent();
    }

    [HttpGet("room/{roomId:guid}/preferences")]
    public async Task<ActionResult<PreferencePayload>> GetRoomPreferences([FromRoute] Guid roomId)
    {
        var payload = await _preferenceService.GetAsync(GetUserId(), roomId);

        return payload is null ? throw new NotFoundException("No preferences set") : Ok(payload);
    }

    [HttpPut("room/{roomId:guid}/preferences")]
    public async Task<IActionResult> PutRoomPreferences(
        [FromRoute] Guid roomId,
        [FromBody] PreferencePayload payload
    )
    {
        await _preferenceService.UpsertAsync(GetUserId(), roomId, payload);

        return NoContent();
    }

    [HttpPost("room/{roomId:guid}/solve")]
    [EnableRateLimiting("optimiser-solve")]
    public async Task<IActionResult> SolveGroup(
        [FromRoute] Guid roomId,
        [FromBody] SolveRequest request
    )
    {
        var userId = GetUserId();
        await _membershipChecker.EnsureMemberAsync(roomId, userId);

        if (!_solveQueue.TryEnqueue(new OptimiserSolveJob(roomId, userId, request, null)))
            throw new ConflictException("A solve you requested is already running for this room");

        await _hub.Clients.Group(roomId.ToString()).ReceiveOptimiserStarted(roomId, userId);

        return Accepted();
    }

    [HttpPost("room/{roomId:guid}/solve/me")]
    [EnableRateLimiting("optimiser-solve")]
    public async Task<IActionResult> SolveSolo(
        [FromRoute] Guid roomId,
        [FromBody] SoloSolveRequest request
    )
    {
        var userId = GetUserId();
        await _membershipChecker.EnsureMemberAsync(roomId, userId);

        if (!_solveQueue.TryEnqueue(new OptimiserSolveJob(roomId, userId, null, request)))
            throw new ConflictException("A solve you requested is already running for this room");

        await _hub.Clients.User(userId.ToString()).ReceiveOptimiserStarted(roomId, userId);

        return Accepted();
    }

    [HttpPost("room/{roomId:guid}/result/save")]
    public async Task<IActionResult> SaveSuggestion(
        [FromRoute] Guid roomId,
        [FromBody] SaveSuggestionRequest request
    )
    {
        var id = await _optimiserService.SaveSuggestionAsync(roomId, GetUserId(), request);

        return Created($"/timetable/{id}", new { id });
    }

    [HttpGet("room/{roomId:guid}/result")]
    public async Task<ActionResult<SolveResponse>> GetResult([FromRoute] Guid roomId) =>
        Ok(await _optimiserService.GetStoredResultAsync(roomId, GetUserId()));
}
