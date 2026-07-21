using Backend.Data;
using Backend.DTOs;
using Backend.DTOs.Mappings;
using Backend.Exceptions;
using Backend.Services.Storage;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace Backend.Services.Profiles;

public class ProfileService(
    AppDbContext context,
    SupabaseStorageClient supabase,
    IGotrueAdminClient<User> adminClient,
    IProfileResponseMapper profileResponseMapper
) : IProfileService
{
    private readonly AppDbContext _context = context;
    private readonly SupabaseStorageClient _supabase = supabase;
    private readonly IGotrueAdminClient<User> _adminClient = adminClient;
    private readonly IProfileResponseMapper _profileResponseMapper = profileResponseMapper;

    public async Task<ProfileResponse> GetUserProfileAsync(Guid userId)
    {
        var profile =
            await _context.Profiles.FirstOrDefaultAsync(p => p.Id == userId)
            ?? throw new NotFoundException("User not found");

        return _profileResponseMapper.ToResponse(profile);
    }

    public async Task<HandleAvailabilityResponse> CheckHandleAvailabilityAsync(
        Guid userId,
        string handle
    )
    {
        var normalizedHandle = ProfileHandleValidator.Normalize(handle);
        var formatError = ProfileHandleValidator.GetFormatError(normalizedHandle);
        if (formatError is not null)
            return new HandleAvailabilityResponse(false, formatError);

        var isTaken = await _context.Profiles.AnyAsync(p =>
            p.Handle == normalizedHandle && p.Id != userId
        );

        return new HandleAvailabilityResponse(
            Available: !isTaken,
            Reason: isTaken ? HandleUnavailableReason.Taken : null
        );
    }

    public async Task UpdateUserProfileAsync(Guid userId, UpdateProfileRequest updatedProfile)
    {
        var username = updatedProfile.Username.Trim();
        var handle = ProfileHandleValidator.Normalize(updatedProfile.Handle);

        var availability = await CheckHandleAvailabilityAsync(userId, handle);
        if (!availability.Available)
            throw new BadRequestException($"Handle is unavailable: {availability.Reason}");

        int rows = await _context
            .Profiles.Where(p => p.Id == userId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(p => p.Username, username).SetProperty(p => p.Handle, handle)
            );

        if (rows == 0)
            throw new NotFoundException("User not found");
    }

    public async Task DeleteUserProfileAsync(Guid userId)
    {
        var profile =
            await _context.Profiles.FindAsync(userId)
            ?? throw new NotFoundException("User not found");

        if (profile.AvatarUpdatedAt is not null)
        {
            await _supabase
                .Client.Storage.From("avatars")
                .Remove([AvatarUrlProvider.GetAvatarPath(userId)]);
        }

        var ownedRoomIds = _context
            .Timetables.Where(t => t.UserId == userId && t.Id == t.RoomId)
            .Select(t => t.Id);
        await _context.Rooms.Where(r => ownedRoomIds.Contains(r.Id)).ExecuteDeleteAsync();

        bool deleted = await _adminClient.DeleteUser(userId.ToString());
        if (!deleted)
            throw new ExternalServiceException($"Failed to delete auth user {userId}.");
    }

    public async Task<ProfileResponse> UpsertUserAvatarAsync(Guid userId, Stream stream)
    {
        using var bitmap =
            SKBitmap.Decode(stream)
            ?? throw new BadRequestException("Invalid or unsupported image format.");

        if (bitmap.Height != bitmap.Width)
            throw new BadRequestException("File should be a square.");

        var profile =
            await _context.Profiles.FindAsync(userId)
            ?? throw new BadRequestException("userId was not found");

        using var resized = bitmap.Resize(
            new SKImageInfo(256, 256),
            new SKSamplingOptions(SKCubicResampler.Mitchell)
        );
        using var image = SKImage.FromBitmap(resized);
        var data = image.Encode(SKEncodedImageFormat.Webp, 75).ToArray();
        var path = AvatarUrlProvider.GetAvatarPath(userId);

        await _supabase
            .Client.Storage.From("avatars")
            .Upload(
                data,
                path,
                new Supabase.Storage.FileOptions { Upsert = true, ContentType = "image/webp" }
            );

        profile.AvatarUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return _profileResponseMapper.ToResponse(profile);
    }

    public async Task DeleteUserAvatarAsync(Guid userId)
    {
        var profile =
            await _context.Profiles.FindAsync(userId)
            ?? throw new NotFoundException("UserId not found");

        if (profile.AvatarUpdatedAt is null)
        {
            // TODO: Log this somewhere
            return;
        }

        var path = AvatarUrlProvider.GetAvatarPath(userId);

        await _supabase.Client.Storage.From("avatars").Remove([path]);

        profile.AvatarUpdatedAt = null;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserCustomisation(
        Guid userId,
        UpdateProfileCustomisationRequest request
    )
    {
        var profile =
            await _context.Profiles.FindAsync(userId)
            ?? throw new NotFoundException("UserId not found");

        profile.Colour = request.Colour;
        profile.DefaultTheme = request.DefaultTheme;

        _context.Profiles.Update(profile);
        await _context.SaveChangesAsync();
    }
}
