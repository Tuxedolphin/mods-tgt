using Backend.Data;
using Backend.Exceptions;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ProfileService(AppDbContext context) : IProfileService
{
    private readonly AppDbContext _context = context;

    public async Task<Profile> GetCurrentUserProfileAsync(Guid userId)
    {
        return await _context.Profiles.FirstOrDefaultAsync(p => p.Id == userId)
            ?? throw new NotFoundException("User not found");
    }

    public async Task UpdateCurrentUserProfileAsync(Guid userId, Profile updatedProfile)
    {
        int rows = await _context
            .Profiles.Where(p => p.Id == userId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.Username, updatedProfile.Username));

        if (rows == 0)
            throw new NotFoundException("User not found");
    }
}
