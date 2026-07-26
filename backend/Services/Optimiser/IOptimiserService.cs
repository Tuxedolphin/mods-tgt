using Backend.DTOs;

namespace Backend.Services.Optimiser;

public interface IOptimiserService
{
    Task<SolveResponse> SolveGroupAsync(Guid roomId, Guid userId, SolveRequest request);
    Task<SolveResponse> SolveSoloAsync(Guid roomId, Guid userId, SoloSolveRequest request);
    Task<SolveResponse> GetStoredResultAsync(Guid roomId, Guid userId);
    Task<Guid> SaveSuggestionAsync(Guid roomId, Guid userId, SaveSuggestionRequest request);
    Task<IReadOnlyList<Guid>> GetRoomAudienceAsync(Guid roomId);
}
