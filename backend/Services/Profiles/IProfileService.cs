using Backend.Models;

namespace Backend.Services.Profiles;

public interface IProfileService
{
    Task<Profile> GetCurrentUserProfileAsync(Guid userId);
    Task UpdateCurrentUserProfileAsync(Guid guid, Profile request);
}
