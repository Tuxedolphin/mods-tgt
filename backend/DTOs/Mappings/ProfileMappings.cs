using Backend.Models;
using Backend.Services.Profiles;

namespace Backend.DTOs.Mappings;

public interface IProfileResponseMapper
{
    ProfileResponse ToResponse(Profile profile);
    RoomMemberResponse ToRoomMemberResponse(Profile profile, RoomRole role, bool isInRoom);
    UserSearchResponse ToUserSearchResponse(Profile profile);
}

public sealed class ProfileResponseMapper(IAvatarUrlProvider avatarUrlProvider)
    : IProfileResponseMapper
{
    private readonly IAvatarUrlProvider _avatarUrlProvider = avatarUrlProvider;

    public ProfileResponse ToResponse(Profile profile) =>
        new(profile.Id, profile.Username, profile.Handle, _avatarUrlProvider.GetAvatarUrl(profile));

    public RoomMemberResponse ToRoomMemberResponse(
        Profile profile,
        RoomRole role,
        bool isInRoom
    )
    {
        var (username, handle) = RequireInitialisedProfile(profile);

        return new(
            profile.Id,
            username,
            handle,
            _avatarUrlProvider.GetAvatarUrl(profile),
            role,
            isInRoom
        );
    }

    public UserSearchResponse ToUserSearchResponse(Profile profile)
    {
        var (username, handle) = RequireInitialisedProfile(profile);

        return new(profile.Id, username, handle, _avatarUrlProvider.GetAvatarUrl(profile));
    }

    private static (string Username, string Handle) RequireInitialisedProfile(Profile profile)
    {
        if (profile.Username is null || profile.Handle is null)
            throw new InvalidOperationException("Profile needs to be properly initialised");

        return (profile.Username, profile.Handle);
    }
}
