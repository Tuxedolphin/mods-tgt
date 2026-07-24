using Backend.Models;
using Backend.Services.Profiles;

namespace Backend.Tests.TestDoubles;

internal sealed class TestAvatarUrlProvider : IAvatarUrlProvider
{
    public string? GetAvatarUrl(Profile profile) =>
        profile.AvatarUpdatedAt is { } updatedAt ? UrlFor(profile.Id, updatedAt) : null;

    public static string UrlFor(Profile profile) =>
        profile.AvatarUpdatedAt is { } updatedAt
            ? UrlFor(profile.Id, updatedAt)
            : throw new ArgumentException("The profile does not have an avatar.", nameof(profile));

    public static string UrlFor(Guid userId, DateTime updatedAt) =>
        $"https://avatars.test/{userId}/avatar.webp?v={updatedAt.Ticks}";
}
