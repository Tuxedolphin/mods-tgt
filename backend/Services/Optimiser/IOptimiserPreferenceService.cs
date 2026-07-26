using Backend.Models;

namespace Backend.Services.Optimiser;

public interface IOptimiserPreferenceService
{
    Task<PreferencePayload?> GetAsync(Guid userId, Guid? roomId);
    Task UpsertAsync(Guid userId, Guid? roomId, PreferencePayload payload);
}
