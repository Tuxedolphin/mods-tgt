using Backend.DTOs;
using Backend.Models;

namespace Backend.Services;

public interface ITimeTableService
{
    Task<List<TimeTableSummaryResponse>> GetTimeTablesAsync(Guid userId);
    Task<TimeTable> GetTimeTableByIdAsync(Guid timeTableId, Guid userId);
    Task<TimeTable> CreateTimeTableAsync(CreateTimeTableRequest request, Guid userId);
    Task UpdateTimeTableAsync(Guid id, UpdateTimeTableRequest request, Guid userId);
    Task DeleteTimeTableAsync(Guid id, Guid userId);
}
