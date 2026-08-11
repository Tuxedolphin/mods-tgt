using Backend.Data;
using Backend.DTOs;
using Backend.Hubs;
using Backend.Hubs.Clients;
using Backend.Exceptions;
using Backend.Models;
using Backend.Services.NusMods;
using Backend.Services.Optimiser;
using Backend.Services.Rooms;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Shouldly;

namespace Backend.Tests.Services;

[Collection(nameof(ServiceTestCollection))]
public class OptimiserServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AppDbContext _context;
    private readonly INusModsClient _nusMods = Substitute.For<INusModsClient>();
    private readonly IRoomTracker _tracker = Substitute.For<IRoomTracker>();
    private readonly OptimiserService _service;

    public OptimiserServiceTests(DatabaseFixture db)
    {
        _db = db;
        _context = db.CreateContext();
        _tracker
            .TryGetTimetablesInRoom(
                Arg.Any<Guid>(),
                out Arg.Any<IReadOnlyCollection<RoomTimetable>>()
            )
            .Returns(false);
        _service = new OptimiserService(
            _context,
            new RoomMembershipChecker(_context),
            new LessonCatalogueBuilder(_nusMods),
            new SolverModelBuilder(),
            _nusMods,
            _tracker,
            Substitute.For<IHubContext<RoomHub, IRoomHubClient>>(),
            Substitute.For<IRoomService>()
        );
    }

    public async Task InitializeAsync() => await _db.ResetAsync();

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private static readonly IReadOnlySet<int> AllWeeks = new HashSet<int>(Enumerable.Range(1, 13));

    private void SetupModule(string code, params NusModsSession[] sessions) =>
        _nusMods.GetModuleAsync("2026-2027", 1, code).Returns(new ModuleTimetable(code, sessions));

    private static NusModsSession S(string classNo, WeekDay day, int start, int end) =>
        new(classNo, "Tutorial", day, start, end, AllWeeks);

    private static TimetableModule Module(string code, string lessonNo) =>
        new()
        {
            ModuleCode = code,
            LessonNo = lessonNo,
            LessonType = "Tutorial",
            Colour = "#123456",
        };

    private async Task<(
        Guid ownerId,
        Guid memberId,
        Guid roomId,
        Guid memberTimetableId
    )> SeedRoomAsync(
        ICollection<TimetableModule> ownerMods,
        ICollection<TimetableModule> memberMods
    )
    {
        var ownerId = await _context.SeedProfileAsync();
        var memberId = await _context.SeedProfileAsync();
        var roomId = Guid.NewGuid();
        var memberTimetableId = Guid.NewGuid();

        _context.Rooms.Add(new Room { Id = roomId, Visibility = Visibility.Restricted });
        _context.Timetables.Add(
            new Timetable
            {
                Id = roomId,
                UserId = ownerId,
                Name = "main",
                Semester = 1,
                AcademicYear = "2026-2027",
                MetaData = ownerMods,
                RoomId = roomId,
            }
        );
        _context.Timetables.Add(
            new Timetable
            {
                Id = memberTimetableId,
                UserId = memberId,
                Name = "copy",
                Semester = 1,
                AcademicYear = "2026-2027",
                MetaData = memberMods,
                RoomId = roomId,
            }
        );
        _context.RoomMembers.Add(
            new RoomMember
            {
                RoomId = roomId,
                UserId = memberId,
                Role = RoomRole.Editor,
            }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return (ownerId, memberId, roomId, memberTimetableId);
    }

    [Fact]
    public async Task SolveGroupAsync_TwoUsersShareableModule_SuggestsSharedSlot()
    {
        SetupModule(
            "CS2100",
            S("T01", WeekDay.Monday, 600, 660),
            S("T02", WeekDay.Tuesday, 600, 660)
        );
        var (ownerId, memberId, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T02")]
        );

        var response = await _service.SolveGroupAsync(
            roomId,
            ownerId,
            new SolveRequest()
        );

        response.Status.ShouldBe("optimal");
        var solution = response.Solutions.First();
        solution
            .Score.SharedClasses.ShouldHaveSingleItem()
            .UserIds.ShouldBe([ownerId, memberId], ignoreOrder: true);
        solution.Suggestions.SelectMany(s => s.Changes).Count().ShouldBe(1);
    }

    [Fact]
    public async Task SolveGroupAsync_NotMember_Forbidden()
    {
        var (_, _, roomId, _) = await SeedRoomAsync([], []);
        var stranger = await _context.SeedProfileAsync();

        await Should.ThrowAsync<ForbiddenException>(
            () => _service.SolveGroupAsync(roomId, stranger, new SolveRequest())
        );
    }

    [Fact]
    public async Task SolveGroupAsync_UnknownModule_WarnsAndSolvesRest()
    {
        SetupModule(
            "CS2100",
            S("T01", WeekDay.Monday, 600, 660),
            S("T02", WeekDay.Tuesday, 600, 660)
        );
        _nusMods.GetModuleAsync("2026-2027", 1, "NEW9999").Returns((ModuleTimetable?)null);

        var (ownerId, _, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01"), Module("NEW9999", "T01")],
            [Module("CS2100", "T02")]
        );

        var response = await _service.SolveGroupAsync(
            roomId,
            ownerId,
            new SolveRequest()
        );

        response.Status.ShouldBe("optimal");
        response.Warnings.ShouldContain(w =>
            w.Code == "moduleNotFound" && w.ModuleCode == "NEW9999"
        );
    }

    [Fact]
    public async Task SolveGroupAsync_TrackerHasNewerData_UsesTrackerTimetables()
    {
        SetupModule(
            "CS2100",
            S("T01", WeekDay.Monday, 600, 660),
            S("T02", WeekDay.Tuesday, 600, 660)
        );
        var (ownerId, memberId, roomId, memberTimetableId) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T01")]
        );

        IReadOnlyCollection<RoomTimetable> live =
        [
            new RoomTimetable
            {
                Id = roomId,
                UserId = ownerId,
                Name = "main",
                Semester = 1,
                AcademicYear = "2026-2027",
                MetaData = [Module("CS2100", "T01")],
                RoomId = roomId,
            },
            new RoomTimetable
            {
                Id = memberTimetableId,
                UserId = memberId,
                Name = "copy",
                Semester = 1,
                AcademicYear = "2026-2027",
                MetaData = [Module("CS2100", "T02")],
                RoomId = roomId,
            },
        ];
        _tracker
            .TryGetTimetablesInRoom(roomId, out Arg.Any<IReadOnlyCollection<RoomTimetable>>())
            .Returns(x =>
            {
                x[1] = live;
                return true;
            });

        var response = await _service.SolveGroupAsync(
            roomId,
            ownerId,
            new SolveRequest()
        );

        response.Solutions.First().Suggestions.SelectMany(s => s.Changes).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task SolveGroupAsync_ReturnsFullDetail_FilterHidesOthersAtDelivery()
    {
        SetupModule("CS2100", S("T01", WeekDay.Monday, 600, 660));
        var (ownerId, memberId, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T01")]
        );

        var response = await _service.SolveGroupAsync(
            roomId,
            ownerId,
            new SolveRequest()
        );

        // The service returns full detail; per-reader filtering happens at delivery.
        var perUser = response.Solutions.First().Score.PerUser;
        perUser.ShouldAllBe(u => u.Satisfied != null);

        var forOwner = OptimiserResultFilter
            .ForReader(response, ownerId)
            .Solutions.First()
            .Score.PerUser;
        forOwner.Single(u => u.UserId == ownerId).Satisfied.ShouldNotBeNull();
        forOwner.Single(u => u.UserId == memberId).Satisfied.ShouldBeNull();
        forOwner.Single(u => u.UserId == memberId).SatisfiedCount.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SolveGroupAsync_NoPreferenceRows_FlagsUsedDefaults()
    {
        SetupModule("CS2100", S("T01", WeekDay.Monday, 600, 660));
        var (ownerId, memberId, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T01")]
        );

        var response = await _service.SolveGroupAsync(
            roomId,
            ownerId,
            new SolveRequest()
        );

        response.UsedDefaults.ShouldBe([ownerId, memberId], ignoreOrder: true);
    }

    [Fact]
    public async Task SolveSoloAsync_OthersFrozen_OnlyCallerMoves()
    {
        SetupModule(
            "CS2100",
            S("T01", WeekDay.Monday, 600, 660),
            S("T02", WeekDay.Tuesday, 600, 660)
        );
        var (ownerId, memberId, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T02")]
        );

        var response = await _service.SolveSoloAsync(
            roomId,
            memberId,
            new SoloSolveRequest()
        );

        var changes = response.Solutions.First().Suggestions;
        changes.ShouldAllBe(s => s.UserId == memberId);
        changes.Single().Changes.Single().To.ShouldBe("T01");
        response.Frozen.ShouldContain(f => f.UserId == ownerId && f.LessonCount == 1);
    }

    [Fact]
    public async Task SolveGroupAsync_IncludeFreeSlots_ReturnsWindows()
    {
        SetupModule("CS2100", S("T01", WeekDay.Monday, 600, 660));
        var (ownerId, _, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T01")]
        );

        var response = await _service.SolveGroupAsync(
            roomId,
            ownerId,
            new SolveRequest { IncludeFreeSlots = true }
        );

        var freeSlots = response.Solutions.First().FreeSlots;
        freeSlots.ShouldNotBeNull();
        freeSlots.ShouldContain(f =>
            f.Day == WeekDay.Monday && f.Start == "0800" && f.End == "1000"
        );
    }

    [Fact]
    public async Task SolveGroupAsync_Completed_PersistsResultRow()
    {
        SetupModule("CS2100", S("T01", WeekDay.Monday, 600, 660));
        var (ownerId, _, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T01")]
        );

        var response = await _service.SolveGroupAsync(roomId, ownerId, new SolveRequest());

        _context.ChangeTracker.Clear();
        var row = _context.OptimiserResults.SingleOrDefault(r =>
            r.RoomId == roomId && r.UserId == null
        );
        row.ShouldNotBeNull();
        row.SolveId.ShouldBe(response.SolveId);
        row.RequestedBy.ShouldBe(ownerId);
    }

    [Fact]
    public async Task SolveGroupAsync_SecondSolve_OverwritesRow()
    {
        SetupModule("CS2100", S("T01", WeekDay.Monday, 600, 660));
        var (ownerId, _, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T01")]
        );

        var first = await _service.SolveGroupAsync(roomId, ownerId, new SolveRequest());
        var second = await _service.SolveGroupAsync(roomId, ownerId, new SolveRequest());

        _context.ChangeTracker.Clear();
        _context.OptimiserResults.Count(r => r.RoomId == roomId && r.UserId == null).ShouldBe(1);
        _context
            .OptimiserResults.Single(r => r.RoomId == roomId && r.UserId == null)
            .SolveId.ShouldBe(second.SolveId);
        second.SolveId.ShouldNotBe(first.SolveId);
    }

    [Fact]
    public async Task GetStoredResultAsync_MemberReads_PrivacyFilteredForReader()
    {
        SetupModule("CS2100", S("T01", WeekDay.Monday, 600, 660));
        var (ownerId, memberId, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T01")]
        );
        await _service.SolveGroupAsync(roomId, ownerId, new SolveRequest());

        var forMember = await _service.GetStoredResultAsync(roomId, memberId);

        var perUser = forMember.Solutions.First().Score.PerUser;
        perUser.Single(u => u.UserId == memberId).Satisfied.ShouldNotBeNull();
        perUser.Single(u => u.UserId == ownerId).Satisfied.ShouldBeNull();
    }

    [Fact]
    public async Task GetStoredResultAsync_NeverSolved_NotFound()
    {
        var (ownerId, _, roomId, _) = await SeedRoomAsync([], []);
        await Should.ThrowAsync<NotFoundException>(
            () => _service.GetStoredResultAsync(roomId, ownerId)
        );
    }

    [Fact]
    public async Task SolveSoloAsync_DoesNotPersist()
    {
        SetupModule("CS2100", S("T01", WeekDay.Monday, 600, 660));
        var (ownerId, _, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T01")]
        );

        await _service.SolveSoloAsync(roomId, ownerId, new SoloSolveRequest());

        _context.ChangeTracker.Clear();
        (await _context.OptimiserResults.FindAsync(roomId)).ShouldBeNull();
    }

    [Fact]
    public async Task SaveSuggestionAsync_Untracked_InsertsNewTimetableRow()
    {
        SetupModule(
            "CS2100",
            S("T01", WeekDay.Monday, 600, 660),
            S("T02", WeekDay.Tuesday, 600, 660)
        );
        var (ownerId, memberId, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T02")]
        );
        var solve = await _service.SolveGroupAsync(roomId, ownerId, new SolveRequest());

        var newId = await _service.SaveSuggestionAsync(
            roomId,
            memberId,
            new SaveSuggestionRequest { SolveId = solve.SolveId, Rank = 1 }
        );

        _context.ChangeTracker.Clear();
        var saved = await _context.Timetables.FindAsync(newId);
        saved.ShouldNotBeNull();
        saved.UserId.ShouldBe(memberId);
        saved.RoomId.ShouldBe(roomId);
        saved.OriginalTimetableId.ShouldBeNull();
        var module = saved.MetaData.ShouldHaveSingleItem();
        module.Colour.ShouldBe("#123456");
    }

    [Fact]
    public async Task SaveSuggestionAsync_StaleSolveId_Conflict()
    {
        SetupModule("CS2100", S("T01", WeekDay.Monday, 600, 660));
        var (ownerId, _, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T01")]
        );
        await _service.SolveGroupAsync(roomId, ownerId, new SolveRequest());

        await Should.ThrowAsync<ConflictException>(
            () =>
                _service.SaveSuggestionAsync(
                    roomId,
                    ownerId,
                    new SaveSuggestionRequest { SolveId = Guid.NewGuid(), Rank = 1 }
                )
        );
    }

    [Fact]
    public async Task SaveSuggestionAsync_TrackedRoom_WritesViaTracker()
    {
        SetupModule("CS2100", S("T01", WeekDay.Monday, 600, 660));
        var (ownerId, _, roomId, _) = await SeedRoomAsync(
            [Module("CS2100", "T01")],
            [Module("CS2100", "T01")]
        );
        var solve = await _service.SolveGroupAsync(roomId, ownerId, new SolveRequest());

        _tracker.RoomExists(roomId).Returns(true);
        _tracker.AddOrUpdateTimetable(Arg.Any<RoomTimetable>()).Returns(true);

        await _service.SaveSuggestionAsync(
            roomId,
            ownerId,
            new SaveSuggestionRequest { SolveId = solve.SolveId, Rank = 1 }
        );

        _tracker
            .Received(1)
            .AddOrUpdateTimetable(
                Arg.Is<RoomTimetable>(t =>
                    t.RoomId == roomId && t.UserId == ownerId && t.OriginalTimetableId == null
                )
            );
        _context.ChangeTracker.Clear();
        _context.Timetables.Count(t => t.RoomId == roomId).ShouldBe(2);
    }
}
