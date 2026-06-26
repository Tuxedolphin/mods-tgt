using Backend.DTOs;
using Backend.Models;

namespace Backend.Services.Profiles;

public interface IProfileService
{
    Task<ProfileResponse> GetCurrentUserProfileAsync(Guid userId);
    Task UpdateCurrentUserProfileAsync(Guid guid, Profile request);
}
