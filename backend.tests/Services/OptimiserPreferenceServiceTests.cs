using Backend.Data;
using Backend.Exceptions;
using Backend.Models;
using Backend.Services.Optimiser;
using Backend.Services.Rooms;
using Shouldly;

namespace Backend.Tests.Services;

[Collection(nameof(ServiceTestCollection))]
public class OptimiserPreferenceServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AppDbContext _context;
    private readonly OptimiserPreferenceService _service;

    public OptimiserPreferenceServiceTests(DatabaseFixture db)
    {
        _db = db;
        _context = db.CreateContext();
        _service = new OptimiserPreferenceService(_context, new RoomMembershipChecker(_context));
    }

    public async Task InitializeAsync() => await _db.ResetAsync();

    public async Task DisposeAsync() => await _context.DisposeAsync();

    private async Task<(Guid ownerId, Guid roomId)> SeedOwnedRoomAsync()
    {
        var ownerId = await _context.SeedProfileAsync();
        var roomId = Guid.NewGuid();
        _context.Rooms.Add(new Room { Id = roomId, Visibility = Visibility.Restricted });
        _context.Timetables.Add(
            new Timetable
            {
                Id = roomId,
                UserId = ownerId,
                Name = "main",
                Semester = 1,
                AcademicYear = "2026-2027",
                MetaData = [],
                RoomId = roomId,
            }
        );
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return (ownerId, roomId);
    }

    [Fact]
    public async Task UpsertAsync_GlobalRow_RoundTrips()
    {
        var userId = await _context.SeedProfileAsync();
        var payload = new PreferencePayload { LunchBreak = Tier.Important };

        await _service.UpsertAsync(userId, null, payload);

        var fetched = await _service.GetAsync(userId, null);
        fetched.ShouldNotBeNull();
        fetched.LunchBreak.ShouldBe(Tier.Important);
    }

    [Fact]
    public async Task UpsertAsync_CalledTwiceGlobal_UpdatesSingleRow()
    {
        var userId = await _context.SeedProfileAsync();

        await _service.UpsertAsync(userId, null, new PreferencePayload { LunchBreak = Tier.Off });
        await _service.UpsertAsync(
            userId,
            null,
            new PreferencePayload { LunchBreak = Tier.Important }
        );

        _context.ChangeTracker.Clear();
        _context.OptimiserPreferences.Count(p => p.UserId == userId).ShouldBe(1);
        (await _service.GetAsync(userId, null))!.LunchBreak.ShouldBe(Tier.Important);
    }

    [Fact]
    public async Task UpsertAsync_RoomRow_OwnerAllowed()
    {
        var (ownerId, roomId) = await SeedOwnedRoomAsync();

        await _service.UpsertAsync(ownerId, roomId, new PreferencePayload());

        (await _service.GetAsync(ownerId, roomId)).ShouldNotBeNull();
    }

    [Fact]
    public async Task UpsertAsync_RoomRow_NonMemberForbidden()
    {
        var (_, roomId) = await SeedOwnedRoomAsync();
        var strangerId = await _context.SeedProfileAsync();

        await Should.ThrowAsync<ForbiddenException>(
            () => _service.UpsertAsync(strangerId, roomId, new PreferencePayload())
        );
    }

    [Fact]
    public async Task UpsertAsync_RoomMissing_NotFound()
    {
        var userId = await _context.SeedProfileAsync();

        await Should.ThrowAsync<NotFoundException>(
            () => _service.UpsertAsync(userId, Guid.NewGuid(), new PreferencePayload())
        );
    }

    [Fact]
    public async Task GetAsync_NoRow_ReturnsNull()
    {
        var userId = await _context.SeedProfileAsync();
        (await _service.GetAsync(userId, null)).ShouldBeNull();
    }
}
