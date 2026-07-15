using Backend.DTOs;
using Backend.DTOs.Mappings;
using Backend.Models;
using Backend.Services.Profiles;
using Shouldly;

namespace Backend.Tests.Unit;

public class ProfileResponseMapperTests
{
    private readonly ProfileResponseMapper _mapper = new(new TestAvatarUrlProvider());

    [Fact]
    public void ToResponse_ProfileHasAvatar_MapsAvatarUrl()
    {
        var profile = MakeProfile(DateTime.UtcNow);

        var result = _mapper.ToResponse(profile);

        result.AvatarUrl.ShouldBe(TestAvatarUrlProvider.UrlFor(profile));
    }

    [Fact]
    public void ToResponse_ProfileHasNoAvatar_MapsNullAvatarUrl()
    {
        var result = _mapper.ToResponse(MakeProfile());

        result.AvatarUrl.ShouldBeNull();
    }

    [Fact]
    public void ToRoomMemberResponse_ProfileHasAvatar_MapsAvatarUrlAndRole()
    {
        var profile = MakeProfile(DateTime.UtcNow);

        var result = _mapper.ToRoomMemberResponse(profile, RoomRole.Viewer, true);

        result.AvatarUrl.ShouldBe(TestAvatarUrlProvider.UrlFor(profile));
        result.Role.ShouldBe(RoomRole.Viewer);
        result.IsInRoom.ShouldBeTrue();
    }

    [Fact]
    public void ToUserSearchResponse_ProfileHasAvatar_MapsAvatarUrl()
    {
        var profile = MakeProfile(DateTime.UtcNow);

        var result = _mapper.ToUserSearchResponse(profile);

        result.AvatarUrl.ShouldBe(TestAvatarUrlProvider.UrlFor(profile));
    }

    private static Profile MakeProfile(DateTime? avatarUpdatedAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Username = "Test user",
            Handle = "test-user",
            AvatarUpdatedAt = avatarUpdatedAt,
        };

    private sealed class TestAvatarUrlProvider : IAvatarUrlProvider
    {
        public string? GetAvatarUrl(Profile profile) =>
            profile.AvatarUpdatedAt is null ? null : UrlFor(profile);

        public static string UrlFor(Profile profile) =>
            $"https://avatars.test/{profile.Id}/avatar.webp?v={profile.AvatarUpdatedAt!.Value.Ticks}";
    }
}
