using Backend.Models;
using Backend.Services.Storage;

namespace Backend.Services.Profiles;

public interface IAvatarUrlProvider
{
    string? GetAvatarUrl(Profile profile);
}

public sealed class AvatarUrlProvider(SupabaseStorageClient supabase) : IAvatarUrlProvider
{
    private const string BucketName = "avatars";
    private readonly SupabaseStorageClient _supabase = supabase;

    public string? GetAvatarUrl(Profile profile) =>
        profile.AvatarUpdatedAt is { } updatedAt
            ? $"{_supabase.Client.Storage.From(BucketName).GetPublicUrl(GetAvatarPath(profile.Id))}?v={updatedAt.Ticks}"
            : null;

    public static string GetAvatarPath(Guid userId) => $"{userId}/avatar.webp";
}
