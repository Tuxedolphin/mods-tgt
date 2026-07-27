using Backend.Data;
using Backend.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Rooms;

public interface IRoomMembershipChecker
{
    Task EnsureMemberAsync(Guid roomId, Guid userId);
}

public class RoomMembershipChecker(AppDbContext context) : IRoomMembershipChecker
{
    public async Task EnsureMemberAsync(Guid roomId, Guid userId)
    {
        var roomExists = await context.Rooms.AnyAsync(r => r.Id == roomId);
        if (!roomExists)
            throw new NotFoundException("Room not found");

        // The owner has no RoomMembers row, ownership comes from the main timetable
        var isMember =
            await context.RoomMembers.AnyAsync(m => m.RoomId == roomId && m.UserId == userId)
            || await context.Timetables.AnyAsync(t => t.Id == roomId && t.UserId == userId);

        if (!isMember)
            throw new ForbiddenException("User is not a member of this room");
    }
}
