using Backend.Data;
using Backend.DTOs;
using Backend.DTOs.Mappings;

using Backend.Exceptions;
using Backend.Infrastructure;
using Backend.Models;
using Backend.Services.Timetables;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Rooms;

public class RoomService(
    ILogger<RoomService> logger,
    IRoomTracker roomTracker,
    IProfileTracker profileTracker,
    ITimetableService timetableService,
    AppDbContext context
) : IRoomService
{
    private readonly ILogger<RoomService> _logger = logger;
    private readonly IRoomTracker _roomTracker = roomTracker;
    private readonly IProfileTracker _profileTracker = profileTracker;
    private readonly ITimetableService _timetableService = timetableService;
    private readonly AppDbContext _context = context;

    public bool RoomExists(Guid roomId)
    {
        return _roomTracker.RoomExists(roomId);
    }

    public async Task<bool> CreateOrJoinRoom(Guid userId, Guid roomId)
    {
        if (!_roomTracker.RoomExists(roomId))
        {
            var timetables =
                await _context
                    .Timetables.Where(t => t.RoomId == roomId)
                    .ToListAsync()
                    .MapAsync(list => list.Select(t => t.ToRoomTimetable()))
                ?? throw new NotFoundException($"Room with roomId {roomId} not found");

            _roomTracker.SetRoom(roomId, [userId], [.. timetables]);
        }

        if (!_profileTracker.GetUserById(userId, out _))
        {
            var profile =
                await _context.Profiles.FirstOrDefaultAsync(p => p.Id == userId)
                ?? throw new NotFoundException($"User with id {userId} not found");

            _profileTracker.SetUser(profile);
        }

        if (_roomTracker.GetRoomOfUser(userId, out Guid oldRoomId) && oldRoomId != roomId)
            await HandleLeaveRoom(userId, oldRoomId);

        if (_roomTracker.AddUserToRoom(userId, roomId))
            return true;

        RoomServiceLogs.LogAttemptedJoinNonExistentRoom(_logger, roomId, userId);
        return false;
    }

    public async Task<bool> HandleLeaveRoom(Guid userId, Guid roomId)
    {
        await CommitChangesAsync(roomId);

        if (_roomTracker.RemoveUserFromRoom(userId, roomId))
        {
            if (_roomTracker.GetUsersInRoom(roomId, out var users) && users.Count > 0)
                return true;

            await CloseRoom(roomId);
        }

        RoomServiceLogs.LogAttemptedLeaveNonExistentRoom(_logger, roomId, userId);
        return false;
    }

    public async Task<RoomInformation?> GetRoomInformationAsync(Guid roomId)
    {
        var profilesOfUsers = await GetProfilesInRoomAsync(roomId);
        var timetablesInformation = await GetTimetablesDetailedInRoomAsync(roomId);

        // BUG: Should query database for info

        if (profilesOfUsers is null || timetablesInformation is null)
            return null;

        return new RoomInformation(roomId, profilesOfUsers, timetablesInformation);
    }

    public async Task<IReadOnlyCollection<ProfileResponse>?> GetProfilesInRoomAsync(Guid roomId)
    {
        if (!_roomTracker.GetUsersInRoom(roomId, out var users))
            return null;

        return await Task.WhenAll(users.ToList().Select(FindOrAddProfileAsync))
            .MapAsync(p => p.OfType<Profile>().Select(profile => profile.ToResponse()).ToList());
    }

    public async Task<IReadOnlyCollection<TimetableDetailedResponse>?> GetTimetablesDetailedInRoomAsync(
        Guid roomId
    )
    {
        if (!_roomTracker.GetTimetablesInRoom(roomId, out var timetables))
            return null;

        return await Task.WhenAll(
                timetables
                    .ToList()
                    .Select(async t =>
                    {
                        var profile = await FindOrAddProfileAsync(t.UserId);
                        return profile == null ? null : t.ToDetailedResponse(profile);
                    })
            )
            .MapAsync(t => t.OfType<TimetableDetailedResponse>().ToList());
    }

    public CreateTimetableResult HandleCreateTimetable(
        Guid roomId,
        Guid userId,
        CreateTimetableRequest timetableRequest,
        Guid? copyOf
    )
    {
        if (!_roomTracker.GetTimetablesInRoom(roomId, out var timetables))
            return CreateTimetableResult.RoomNotFound;

        if (copyOf is not null && timetables.Any(t => t.OriginalTimetableId == copyOf))
            return CreateTimetableResult.TImetableIdConflict;

        _roomTracker.AddOrUpdateTimetable(
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = timetableRequest.Name,
                Semester = timetableRequest.Semester,
                AcademicYear = timetableRequest.AcademicYear,
                MetaData = timetableRequest.MetaData,
                OriginalTimetableId = copyOf,
                RoomId = roomId,
            }
        );

        return CreateTimetableResult.Success;
    }

    public async Task<bool> HandleUpdateTimetableAsync(
        Guid roomId,
        Guid timetableId,
        UpdateTimetableRequest timetableRequest
    )
    {
        var timetable =
            _roomTracker.GetTimetableById(roomId, timetableId)
            ?? (
                await _context.Timetables.FirstOrDefaultAsync(t =>
                    t.Id == timetableId && t.RoomId == roomId
                )
            )?.ToRoomTimetable();

        if (timetable is null)
            return false;

        _roomTracker.AddOrUpdateTimetable(timetable.ApplyUpdate(timetableRequest));
        return true;
    }

    // Will not add/ update existing profiles
    public async Task<bool> AddProfileAsync(Guid userId)
    {
        if (_profileTracker.GetUserById(userId, out _))
            return true;

        var profile = await _context.Profiles.FirstOrDefaultAsync(t => t.Id == userId);
        if (profile is null)
            return false;

        _profileTracker.SetUser(profile);
        return true;
    }

    private async Task<Profile?> FindOrAddProfileAsync(Guid userId)
    {
        if (_profileTracker.GetUserById(userId, out Profile? profile))
            return profile;

        profile = await _context.Profiles.FirstOrDefaultAsync(t => t.Id == userId);
        if (profile is null)
            return null;

        _profileTracker.SetUser(profile);
        return profile;
    }

    public async Task<bool> CloseRoom(Guid roomId)
    {
        if (!_roomTracker.GetUsersInRoom(roomId, out var users))
            return false;

        return _profileTracker.RemoveUsers(users) && _roomTracker.CloseRoom(roomId);
    }

    public bool HandleDeleteTimetable(Guid roomId, Guid timetableId)
    {
        // The main timetable is the one the roomId is named after
        if (roomId == timetableId)
            return false;

        _roomTracker.DeleteTimetable(roomId, timetableId);
        return true;

        // TODO: Add logging
    }

    public async Task<bool> CommitChangesAsync(Guid roomId)
    {
        try
        {
            if (_roomTracker.GetChangedTimetables(roomId, out var changedTimetables))
                await changedTimetables.ForEachAsync(_timetableService.UpsertTimetableAsync);

            if (_roomTracker.GetDeletedTimetables(roomId) is { } deletedTimetables)
            {
                await deletedTimetables.ForEachAsync(_timetableService.FlushDeleteTimetableAsync);
                _roomTracker.RemoveTimetablesFromDeleted(roomId, deletedTimetables);
            }

            await _context.SaveChangesAsync();
            _roomTracker.RemoveTimetablesFromChanged([.. changedTimetables.Select(t => t.Id)]);

            return true;
        }
        catch (InvalidOperationException e)
        {
            RoomServiceLogs.LogCommitInvalidOperation(_logger, e, roomId);
            return false;
        }
        catch (DbUpdateException e)
        {
            RoomServiceLogs.LogCommitDbUpdateFailed(_logger, e, roomId);
            return false;
        }
    }
}

public enum CreateTimetableResult
{
    Success,
    RoomNotFound,
    TImetableIdConflict,
}
