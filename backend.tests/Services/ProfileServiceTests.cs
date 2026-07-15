using Backend.Data;
using Backend.DTOs;
using Backend.DTOs.Mappings;
using Backend.Services.Profiles;
using NSubstitute;
using Shouldly;

namespace Backend.Tests.Services;

[Collection(nameof(ServiceTestCollection))]
public class ProfileServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly AppDbContext _context;
    private readonly ProfileService _service;

    public ProfileServiceTests(DatabaseFixture db)
    {
        _db = db;
        _context = db.CreateContext();
        _service = new ProfileService(
            _context,
            null!,
            null!,
            Substitute.For<IProfileResponseMapper>()
        );
    }

    public async Task InitializeAsync() => await _db.ResetAsync();

    public async Task DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task CheckHandleAvailability_InvalidHandle_ReturnsFormatReason()
    {
        var result = await _service.CheckHandleAvailabilityAsync(
            Guid.NewGuid(),
            "invalid--handle"
        );

        result.ShouldBe(
            new HandleAvailabilityResponse(false, HandleUnavailableReason.InvalidFormat)
        );
    }

    [Fact]
    public async Task CheckHandleAvailability_HandleUsedByAnotherUser_ReturnsTaken()
    {
        await _context.SeedProfileAsync(handle: "existing-handle");

        var result = await _service.CheckHandleAvailabilityAsync(
            Guid.NewGuid(),
            " Existing-Handle "
        );

        result.ShouldBe(new HandleAvailabilityResponse(false, HandleUnavailableReason.Taken));
    }

    [Fact]
    public async Task CheckHandleAvailability_CurrentUsersHandle_ReturnsAvailable()
    {
        var userId = await _context.SeedProfileAsync(handle: "current-handle");

        var result = await _service.CheckHandleAvailabilityAsync(userId, "current-handle");

        result.ShouldBe(new HandleAvailabilityResponse(true, null));
    }
}
