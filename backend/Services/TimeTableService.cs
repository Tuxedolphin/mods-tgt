using Backend.DTOs;
using Backend.Models;

namespace Backend.Services;

public class TimeTableService : ITimeTableService
{
    public async Task<TimeTable> CreateTimeTableAsync(TimeTable timeTable)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteTimeTableAsync(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<TimeTable> GetTimeTableByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<List<TimeTableSummaryResponse>> GetTimeTablesAsync()
    {
        throw new NotImplementedException();
    }

    public async Task UpdateTimeTableAsync(int id, TimeTable timeTable)
    {
        throw new NotImplementedException();
    }
}
