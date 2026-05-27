using Backend.DTOs;
using Backend.Models;

namespace Backend.Services;

public interface ITimeTableService
{
    Task<List<TimeTableSummaryResponse>> GetTimeTablesAsync();
    Task<TimeTable> GetTimeTableByIdAsync(int id);
    Task<TimeTable> CreateTimeTableAsync(TimeTable timeTable);
    Task UpdateTimeTableAsync(int id, TimeTable timeTable);
    Task DeleteTimeTableAsync(int id);
}
