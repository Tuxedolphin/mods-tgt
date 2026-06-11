using Backend.DTOs;
using Backend.Models;

namespace Backend.Services.Timetables;

public interface ITimetableService
{
    Task<List<TimetableSummaryResponse>> GetTimetablesAsync(Guid userId);
    Task<Timetable> GetTimetableByIdAsync(Guid timetableId, Guid userId);
    Task<Timetable> CreateTimetableAsync(CreateTimetableRequest request, Guid userId);
    Task UpdateTimetableAsync(Guid id, UpdateTimetableRequest request, Guid userId);
    Task DeleteTimetableAsync(Guid id, Guid userId);
}
