using System.Security.Claims;
using Backend.Controllers;
using Backend.DTOs;
using Backend.Exceptions;
using Backend.Hubs;
using Backend.Hubs.Clients;
using Backend.Models;
using Backend.Services.Optimiser;
using Backend.Services.Rooms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Shouldly;

namespace Backend.Tests.Unit;

public class OptimiserControllerTests
{
    private readonly IOptimiserPreferenceService _preferences =
        Substitute.For<IOptimiserPreferenceService>();
    private readonly IOptimiserService _optimiser = Substitute.For<IOptimiserService>();
    private readonly IRoomMembershipChecker _membership =
        Substitute.For<IRoomMembershipChecker>();
    private readonly IOptimiserSolveQueue _queue = Substitute.For<IOptimiserSolveQueue>();
    private readonly IHubContext<RoomHub, IRoomHubClient> _hub = Substitute.For<
        IHubContext<RoomHub, IRoomHubClient>
    >();

    private OptimiserController CreateController(Guid userId) =>
        new(_preferences, _optimiser, _membership, _queue, _hub)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                            "test"
                        )
                    ),
                },
            },
        };

    private static SolveResponse EmptyResponse() =>
        new()
        {
            Status = "optimal",
            SolveId = Guid.NewGuid(),
            Solutions = [],
            Warnings = [],
            UsedDefaults = [],
            Frozen = [],
        };

    [Fact]
    public async Task GetGlobalPreferences_NoRow_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        _preferences.GetAsync(userId, null).Returns((PreferencePayload?)null);

        var controller = CreateController(userId);

        await Should.ThrowAsync<NotFoundException>(() => controller.GetGlobalPreferences());
    }

    [Fact]
    public async Task GetRoomPreferences_RowExists_ReturnsPayload()
    {
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var payload = new PreferencePayload { LunchBreak = Tier.Important };
        _preferences.GetAsync(userId, roomId).Returns(payload);

        var controller = CreateController(userId);

        var result = await controller.GetRoomPreferences(roomId);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBe(payload);
    }

    [Fact]
    public async Task PutGlobalPreferences_UpsertsForCallerAndReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        var payload = new PreferencePayload { CompactDays = Tier.NiceToHave };

        var controller = CreateController(userId);

        var result = await controller.PutGlobalPreferences(payload);

        result.ShouldBeOfType<NoContentResult>();
        await _preferences.Received(1).UpsertAsync(userId, null, payload);
    }

    [Fact]
    public async Task SolveGroup_Enqueued_ReturnsAccepted()
    {
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var request = new SolveRequest();
        _queue.TryEnqueue(Arg.Any<OptimiserSolveJob>()).Returns(true);

        var controller = CreateController(userId);

        var result = await controller.SolveGroup(roomId, request);

        result.ShouldBeOfType<AcceptedResult>();
        await _membership.Received(1).EnsureMemberAsync(roomId, userId);
        _queue
            .Received(1)
            .TryEnqueue(
                Arg.Is<OptimiserSolveJob>(j =>
                    j.RoomId == roomId
                    && j.CallerId == userId
                    && j.GroupRequest == request
                    && j.SoloRequest == null
                )
            );
    }

    [Fact]
    public async Task SolveGroup_AlreadyInFlight_ThrowsConflict()
    {
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        _queue.TryEnqueue(Arg.Any<OptimiserSolveJob>()).Returns(false);

        var controller = CreateController(userId);

        await Should.ThrowAsync<ConflictException>(
            () => controller.SolveGroup(roomId, new SolveRequest())
        );
    }

    [Fact]
    public async Task SolveGroup_NotMember_DoesNotEnqueue()
    {
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        _membership
            .EnsureMemberAsync(roomId, userId)
            .Returns(x => throw new ForbiddenException("User is not a member of this room"));

        var controller = CreateController(userId);

        await Should.ThrowAsync<ForbiddenException>(
            () => controller.SolveGroup(roomId, new SolveRequest())
        );
        _queue.DidNotReceive().TryEnqueue(Arg.Any<OptimiserSolveJob>());
    }

    [Fact]
    public async Task SolveSolo_Enqueued_ReturnsAccepted()
    {
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var request = new SoloSolveRequest();
        _queue.TryEnqueue(Arg.Any<OptimiserSolveJob>()).Returns(true);

        var controller = CreateController(userId);

        var result = await controller.SolveSolo(roomId, request);

        result.ShouldBeOfType<AcceptedResult>();
        _queue
            .Received(1)
            .TryEnqueue(
                Arg.Is<OptimiserSolveJob>(j =>
                    j.RoomId == roomId
                    && j.CallerId == userId
                    && j.GroupRequest == null
                    && j.SoloRequest == request
                )
            );
    }

    [Fact]
    public async Task GetResult_ReturnsStoredResultForCaller()
    {
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var expected = EmptyResponse();
        _optimiser.GetStoredResultAsync(roomId, userId).Returns(expected);

        var controller = CreateController(userId);

        var result = await controller.GetResult(roomId);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBe(expected);
    }

    [Fact]
    public async Task SaveSuggestion_ReturnsCreatedWithNewTimetableId()
    {
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var request = new SaveSuggestionRequest { SolveId = Guid.NewGuid(), Rank = 1 };
        _optimiser.SaveSuggestionAsync(roomId, userId, request).Returns(newId);

        var controller = CreateController(userId);

        var result = await controller.SaveSuggestion(roomId, request);

        var created = result.ShouldBeOfType<CreatedResult>();
        created.Location.ShouldBe($"/timetable/{newId}");
    }
}
