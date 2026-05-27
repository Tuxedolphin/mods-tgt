using Backend.Data;
using Backend.DTOs;
using Backend.Exceptions;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class TimeTableService(AppDbContext context) : ITimeTableService
{
    private readonly AppDbContext _context = context;

    public async Task<TimeTable> CreateTimeTableAsync(CreateTimeTableRequest request, Guid userId)
    {
        TimeTable timeTable = new TimeTable
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
        TimeTable timeTable = await GetTimeTableByIdAsync(timeTableId, userId);
        _context.TimeTables.Remove(timeTable);
        await _context.SaveChangesAsync();
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

    public async Task UpdateTimeTableAsync(Guid id, TimeTable timeTable, Guid userId)
    {
        TimeTable existingTimeTable = await GetTimeTableByIdAsync(id, userId);

        existingTimeTable.Name = timeTable.Name;
        existingTimeTable.Semester = timeTable.Semester;
        existingTimeTable.AcademicYear = timeTable.AcademicYear;

        existingTimeTable.MetaData = timeTable.MetaData;

        _context.TimeTables.Update(existingTimeTable);
        await _context.SaveChangesAsync();
    }
}
