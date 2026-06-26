using Backend.Models;

namespace Backend.DTOs.Mappings;

public static class ProfileMappings
{
    public static ProfileResponse ToResponse(this Profile profile) =>
        new(profile.Id, profile.Username);
}
