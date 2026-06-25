using Backend.Data;
using Backend.DTOs;
using Backend.DTOs.Mappings;
using Backend.Exceptions;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Timetables;

public class TimetableService(AppDbContext context) : ITimetableService
{
    private readonly AppDbContext _context = context;

    public async Task<TimetableResponse> CreateTimetableAsync(
        CreateTimetableRequest request,
        Guid userId
    )
    {
        // We explicitly set the roomId as the same as the "main" timetable's Id
        var newId = Guid.NewGuid();

        Room room = new() { Id = newId };
        _context.Rooms.Add(room);

        Timetable timetable = new()
        {
            Id = newId,
            Name = request.Name,
            Semester = request.Semester,
            AcademicYear = request.AcademicYear,
            MetaData = request.MetaData,
            UserId = userId,
            RoomId = newId,
        };

        _context.Timetables.Add(timetable);
        await _context.SaveChangesAsync();

        return timetable.ToResponse();
    }

    public async Task DeleteTimetableAsync(Guid timetableId, Guid userId)
    {
        var timetable = await _context.Timetables.FindAsync(timetableId);

        if (timetable is null || timetable.UserId != userId)
            throw new NotFoundException($"Timetable with id {timetableId} not found");

        // This is the case if the timetable is the "main" timetable of the room
        if (timetable.RoomId == timetable.Id)
        {
            await _context.Rooms.Where(r => r.Id == timetable.RoomId).ExecuteDeleteAsync();
        }
        else
        {
            _context.Timetables.Remove(timetable);
            await _context.SaveChangesAsync();
        }
    }

    // Method for handling auto saving of deleted timetables in rooms
    public async Task FlushDeleteTimetableAsync(Guid timetableId)
    {
        // We don't want to delete the "main" timetable from here
        if (await _context.Timetables.AnyAsync(t => t.Id == timetableId && t.Id == t.RoomId))
        {
            throw new InvalidOperationException(
                $"Attempted to flush-delete main timetable {timetableId}"
            );
        }

        await _context.Timetables.Where(t => t.Id == timetableId).ExecuteDeleteAsync();
    }

    public async Task<TimetableResponse> GetTimetableByIdAsync(Guid timetableId, Guid userId)
    {
        Timetable timetable =
            await _context.Timetables.FirstOrDefaultAsync(t =>
                t.Id == timetableId && t.UserId == userId
            )
            ?? throw new NotFoundException(
                $"Timetable with id {timetableId} belonging to userId {userId} not found."
            );

        return timetable.ToResponse();
    }

    public async Task<List<TimetableSummaryResponse>> GetTimetablesAsync(Guid userId)
    {
        return await _context
            .Timetables.Where(t => t.UserId == userId) // TODO: have some sort of friend system
            .Select(t => new TimetableSummaryResponse
            {
                Id = t.Id,
                Name = t.Name,
                Semester = t.Semester,
                AcademicYear = t.AcademicYear,
                CreatedAt = t.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task UpdateTimetableAsync(Guid id, UpdateTimetableRequest request, Guid userId)
    {
        var timetable =
            await _context.Timetables.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId)
            ?? throw new NotFoundException($"Timetable with id {id} not found");

        timetable.Name = request.Name;
        timetable.MetaData = [.. request.MetaData];

        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpsertTimetableAsync(RoomTimetable timetable)
    {
        var existing = await _context.Timetables.FirstOrDefaultAsync(t => t.Id == timetable.Id);

        if (existing is null)
        {
            _context.Timetables.Add(
                new Timetable
                {
                    Id = timetable.Id,
                    UserId = timetable.UserId,
                    Name = timetable.Name,
                    Semester = timetable.Semester,
                    AcademicYear = timetable.AcademicYear,
                    MetaData = [.. timetable.MetaData],
                    RoomId = timetable.RoomId,
                    OriginalTimetableId = timetable.OriginalTimetableId,
                }
            );
        }
        else
        {
            existing.Name = timetable.Name;
            existing.Semester = timetable.Semester;
            existing.AcademicYear = timetable.AcademicYear;
            existing.MetaData = [.. timetable.MetaData];
            existing.OriginalTimetableId = timetable.OriginalTimetableId;
        }

        return true;
    }
}
