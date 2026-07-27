using Backend.Data;
using Backend.Models;
using Backend.Services.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Optimiser;

public class OptimiserPreferenceService(
    AppDbContext context,
    IRoomMembershipChecker membershipChecker
) : IOptimiserPreferenceService
{
    public async Task<PreferencePayload?> GetAsync(Guid userId, Guid? roomId)
    {
        if (roomId is { } id)
            await membershipChecker.EnsureMemberAsync(id, userId);

        var row = await context
            .OptimiserPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.RoomId == roomId);

        return row?.Payload;
    }

    public async Task UpsertAsync(Guid userId, Guid? roomId, PreferencePayload payload)
    {
        if (roomId is { } id)
            await membershipChecker.EnsureMemberAsync(id, userId);

        var row = await context.OptimiserPreferences.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.RoomId == roomId
        );

        if (row is null)
        {
            context.OptimiserPreferences.Add(
                new OptimiserPreference
                {
                    UserId = userId,
                    RoomId = roomId,
                    Payload = payload,
                }
            );
        }
        else
        {
            row.Payload = payload;
        }

        await context.SaveChangesAsync();
    }
}
