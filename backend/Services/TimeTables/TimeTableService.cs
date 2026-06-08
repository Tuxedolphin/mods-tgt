using Backend.Data;
using Backend.DTOs;
using Backend.Exceptions;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.TimeTables;

public class TimeTableService(AppDbContext context) : ITimeTableService
{
    private readonly AppDbContext _context = context;

    public async Task<TimeTable> CreateTimeTableAsync(CreateTimeTableRequest request, Guid userId)
    {
        TimeTable timeTable = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Semester = request.Semester,
            AcademicYear = request.AcademicYear,
            UserId = userId,
            MetaData = request.MetaData,
        };

        _context.TimeTables.Add(timeTable);
        await _context.SaveChangesAsync();

        return timeTable;
    }

    public async Task DeleteTimeTableAsync(Guid timeTableId, Guid userId)
    {
        var rows = await _context
            .TimeTables.Where(t => t.Id == timeTableId && t.UserId == userId)
            .ExecuteDeleteAsync();

        if (rows == 0)
            throw new NotFoundException($"Timetable with id {timeTableId} not found");
    }

    public async Task<TimeTable> GetTimeTableByIdAsync(Guid timeTableId, Guid userId)
    {
        return await _context.TimeTables.FirstOrDefaultAsync(t =>
                t.Id == timeTableId && t.UserId == userId
            )
            ?? throw new NotFoundException(
                $"TimeTable with id {timeTableId} belonging to userId {userId} not found."
            );
    }

    public async Task<List<TimeTableSummaryResponse>> GetTimeTablesAsync(Guid userId)
    {
        return await _context
            .TimeTables.Where(t => t.UserId == userId)
            .Select(t => new TimeTableSummaryResponse
            {
                Id = t.Id,
                Name = t.Name,
                Semester = t.Semester,
                AcademicYear = t.AcademicYear,
                CreatedAt = t.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task UpdateTimeTableAsync(Guid id, UpdateTimeTableRequest request, Guid userId)
    {
        var timetable =
            await _context.TimeTables.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId)
            ?? throw new NotFoundException($"Timetable with id {id} not found");

        timetable.Name = request.Name;
        timetable.MetaData = request.MetaData;

        await _context.SaveChangesAsync();
    }
}
