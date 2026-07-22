using Backend.DTOs;
using Backend.Services.Rooms;

namespace Backend.Services.Timetables;

public interface ITimetableService
{
    Task<List<TimetableSummaryResponse>> GetTimetablesAsync(Guid userId);

    Task<TimetableResponse> GetTimetableByIdAsync(Guid timetableId, Guid userId);
    Task<TimetableResponse> CreateTimetableAsync(CreateTimetableRequest request, Guid userId);

    Task UpdateTimetableAsync(Guid id, UpdateTimetableRequest request, Guid userId);
    Task DeleteTimetableAsync(Guid id, Guid userId);
    Task<bool> UpsertTimetableAsync(RoomTimetable timetable);
    Task FlushDeleteTimetableAsync(Guid timetableId);

    Task<List<TimetableSummaryResponse>> GetSharedTimetablesAsync(Guid userId);
}
