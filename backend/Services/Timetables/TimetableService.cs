using Backend.Data;
using Backend.DTOs;
using Backend.Exceptions;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Timetables;

public class TimetableService(AppDbContext context) : ITimetableService
{
    private readonly AppDbContext _context = context;

    public async Task<Timetable> CreateTimetableAsync(CreateTimetableRequest request, Guid userId)
    {
        Timetable timetable = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Semester = request.Semester,
            AcademicYear = request.AcademicYear,
            UserId = userId,
            MetaData = request.MetaData,
        };

        _context.Timetables.Add(timetable);
        await _context.SaveChangesAsync();

        return timetable;
    }

    public async Task DeleteTimetableAsync(Guid timetableId, Guid userId)
    {
        var rows = await _context
            .Timetables.Where(t => t.Id == timetableId && t.UserId == userId)
            .ExecuteDeleteAsync();

        if (rows == 0)
            throw new NotFoundException($"Timetable with id {timetableId} not found");
    }

    public async Task<Timetable> GetTimetableByIdAsync(Guid timetableId, Guid userId)
    {
        return await _context.Timetables.FirstOrDefaultAsync(t =>
                t.Id == timetableId && t.UserId == userId
            )
            ?? throw new NotFoundException(
                $"Timetable with id {timetableId} belonging to userId {userId} not found."
            );
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
        timetable.MetaData = request.MetaData;

        await _context.SaveChangesAsync();
    }
}
