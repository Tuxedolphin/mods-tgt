using Backend.Models;

namespace Backend.DTOs.Mappings;

public static class ProfileMappings
{
    public static ProfileResponse ToResponse(this Profile profile, string? avatarUrl = null)
    {
        // We do a defensive check here to make sure that if an avatar exists,
        // the url is passed in here and vice versa

        if (profile.AvatarUpdatedAt == null != (avatarUrl == null))
        {
            throw new ArgumentException(
                "avatarUrl must be provided if and only if the profile has an AvatarUpdatedAt value.",
                nameof(avatarUrl)
            );
        }

        return new(profile.Id, profile.Username, avatarUrl);
    }

    public static RoomMemberResponse ToRoomMemberResponse(this Profile profile, RoomRole role) =>
        new(profile.Id, profile.Username, null, role);
}
