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
    IProfileResponseMapper profileResponseMapper,
    AppDbContext context
) : IRoomService
{
    private readonly ILogger<RoomService> _logger = logger;
    private readonly IRoomTracker _roomTracker = roomTracker;
    private readonly IProfileTracker _profileTracker = profileTracker;
    private readonly ITimetableService _timetableService = timetableService;
    private readonly IProfileResponseMapper _profileResponseMapper = profileResponseMapper;
    private readonly AppDbContext _context = context;

    public bool RoomExists(Guid roomId)
    {
        return _roomTracker.RoomExists(roomId);
    }

    public async Task RegisterConnectionAsync(Guid userId, string connectionId)
    {
        if (!_roomTracker.RegisterConnection(connectionId, userId))
        {
            throw new InvalidOperationException(
                $"Connection {connectionId} is already registered."
            );
        }

        try
        {
            if (await AddProfileAsync(userId))
                return;
        }
        catch
        {
            _roomTracker.RemoveConnection(connectionId);
            throw;
        }

        _roomTracker.RemoveConnection(connectionId);
        throw new NotFoundException($"User with id {userId} not found");
    }

    public async Task<RoomConnectionMove> CreateOrJoinRoomAsync(
        Guid roomId,
        Guid userId,
        string connectionId
    )
    {
        if (!_roomTracker.RoomExists(roomId))
            await LoadRoomIntoTrackerAsync(roomId);

        var move = _roomTracker.MoveConnectionToRoom(connectionId, userId, roomId);
        if (move.PreviousRoomId is { } previousRoomId && previousRoomId != roomId)
        {
            await FinaliseRoomDepartureAsync(previousRoomId);
        }

        return move;
    }

    public async Task<RoomConnectionDeparture?> HandleLeaveRoomAsync(
        Guid roomId,
        string connectionId
    )
    {
        var departure = _roomTracker.LeaveConnectionFromRoom(connectionId, roomId);
        if (departure is null)
            return null;

        await FinaliseRoomDepartureAsync(roomId);
        return departure;
    }

    public async Task<ConnectionRemoval?> HandleDisconnectAsync(string connectionId)
    {
        var removal = _roomTracker.RemoveConnection(connectionId);
        if (removal is null)
            return null;

        if (removal.RoomId is { } roomId)
            await FinaliseRoomDepartureAsync(roomId);

        if (removal.WasLastConnectionForUser)
            _profileTracker.RemoveUser(removal.UserId);

        return removal;
    }

    public bool TryGetRoomOfConnection(string connectionId, out Guid roomId) =>
        _roomTracker.TryGetRoomOfConnection(connectionId, out roomId);

    public bool IsConnectionInRoom(string connectionId, Guid roomId) =>
        _roomTracker.IsConnectionInRoom(connectionId, roomId);

    private async Task FinaliseRoomDepartureAsync(Guid roomId)
    {
        // TODO: Add a retry/expiry policy so rooms with permanently failing commits
        // don't linger forever.

        var committed = await CommitChangesAsync(roomId);

        if (committed && _roomTracker.TryGetUsersInRoom(roomId, out var users) && users.Count == 0)
            CloseRoom(roomId);
    }

    public async Task<IReadOnlyCollection<string>> UpdateRoomVisibilityAsync(
        Guid roomId,
        Guid callerId,
        Visibility visibility
    )
    {
        if (!Enum.IsDefined(visibility))
        {
            throw new ArgumentOutOfRangeException(
                nameof(visibility),
                visibility,
                "The room visibility is invalid."
            );
        }

        if (!IsOwner(roomId, callerId))
        {
            throw new UnauthorizedAccessException(
                "Only the owner of a room can change the room visibility"
            );
        }

        var room =
            await _context.Rooms.FindAsync(roomId)
            ?? throw new NotFoundException($"Room with roomId {roomId} was not found");

        room.Visibility = visibility;
        await _context.SaveChangesAsync();

        if (!_roomTracker.UpdateRoomVisibility(roomId, visibility, out var removedConnectionIds))
        {
            throw new InvalidOperationException(
                "Room needs to be initialised first before editing"
            );
        }

        return removedConnectionIds;
    }

    public async Task<RoomInformation?> GetRoomInformationAsync(Guid roomId, Guid userId)
    {
        var members = await GetRoomMembersAsync(roomId, userId);
        var timetablesInformation = await GetTimetablesDetailedInRoomAsync(roomId, userId);

        if (
            members is null
            || timetablesInformation is null
            || !_roomTracker.TryGetVisibilityOfRoom(roomId, out var visibility)
        )
        {
            return null;
        }

        return new RoomInformation(roomId, members, timetablesInformation, visibility);
    }

    public async Task<IReadOnlyCollection<RoomMemberResponse>?> GetRoomMembersAsync(
        Guid roomId,
        Guid userId
    )
    {
        if (!_roomTracker.TryGetUsersInRoom(roomId, out var usersInRoom))
            return null;

        RequireViewPermission(roomId, userId);

        var ownerId =
            _roomTracker.GetTimetableById(roomId, roomId)?.UserId
            ?? throw new InvalidOperationException($"Room {roomId} has no main timetable.");

        var roles = await _context
            .RoomMembers.Where(member => member.RoomId == roomId)
            .ToDictionaryAsync(member => member.UserId, member => member.Role);
        roles[ownerId] = RoomRole.Owner;

        var memberIds = roles.Keys.ToList();
        var usersInRoomSet = usersInRoom.ToHashSet();

        var profilesById = await _context
            .Profiles.AsNoTracking()
            .Where(profile => memberIds.Contains(profile.Id))
            .ToDictionaryAsync(p => p.Id);

        return
        [
            .. roles
                .Select(member =>
                    profilesById.TryGetValue(member.Key, out var profile)
                        ? _profileResponseMapper.ToRoomMemberResponse(
                            profile,
                            member.Value,
                            usersInRoomSet.Contains(member.Key)
                        )
                        : null
                )
                .OfType<RoomMemberResponse>()
                .OrderBy(member => member.Role)
                .ThenBy(member => member.Handle),
        ];
    }

    public async Task<IReadOnlyCollection<TimetableDetailedResponse>?> GetTimetablesDetailedInRoomAsync(
        Guid roomId,
        Guid userId
    )
    {
        if (!_roomTracker.TryGetTimetablesInRoom(roomId, out var timetables))
            return null;

        RequireViewPermission(roomId, userId);

        var ownerIds = timetables.Select(t => t.UserId).ToHashSet();
        var profilesById = await _context
            .Profiles.AsNoTracking()
            .Where(p => ownerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        return
        [
            .. timetables
                .Select(t =>
                    profilesById.TryGetValue(t.UserId, out var profile)
                        ? t.ToDetailedResponse(_profileResponseMapper.ToResponse(profile))
                        : null
                )
                .OfType<TimetableDetailedResponse>(),
        ];
    }

    public CreateTimetableResult HandleCreateTimetable(
        Guid roomId,
        Guid userId,
        CreateTimetableRequest timetableRequest,
        Guid? copyOf
    )
    {
        if (!_roomTracker.TryGetTimetablesInRoom(roomId, out var timetables))
            return CreateTimetableResult.RoomNotFound;

        RequireEditPermission(roomId, userId);

        if (copyOf is not null && timetables.Any(t => t.OriginalTimetableId == copyOf))
            return CreateTimetableResult.TimetableIdConflict;

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
        Guid userId,
        Guid timetableId,
        UpdateTimetableRequest timetableRequest
    )
    {
        if (!_roomTracker.RoomExists(roomId))
            return false;

        RequireEditPermission(roomId, userId);

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
    private async Task<bool> AddProfileAsync(Guid userId)
    {
        if (_profileTracker.TryGetUserById(userId, out _))
            return true;

        var profile = await _context.Profiles.FirstOrDefaultAsync(t => t.Id == userId);
        if (profile is null)
            return false;

        _profileTracker.SetUser(profile);
        return true;
    }

    public bool CloseRoom(Guid roomId) => _roomTracker.CloseRoom(roomId);

    public bool HandleDeleteTimetable(Guid roomId, Guid userId, Guid timetableId)
    {
        // The main timetable is the one the roomId is named after
        if (roomId == timetableId)
            return false;

        RequireEditPermission(roomId, userId);

        _roomTracker.DeleteTimetable(roomId, timetableId);
        return true;

        // TODO: Add logging
    }

    public async Task<bool> CommitChangesAsync(Guid roomId)
    {
        try
        {
            if (_roomTracker.TryGetChangedTimetables(roomId, out var changedTimetables))
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

    public async Task<IReadOnlyCollection<UserSearchResponse>> FindUsersByHandleAsync(
        string handle,
        Guid roomId,
        Guid callerId
    )
    {
        const int maxResults = 10;

        if (string.IsNullOrWhiteSpace(handle) || handle.Length < 2)
            return [];

        RequireViewPermission(roomId, callerId);

        if (!_roomTracker.TryGetUsersInRoom(roomId, out var currentMembersInRoom))
        {
            throw new InvalidOperationException(
                "Room should be tracked and cached before operations"
            );
        }

        var existingMemberIds = await _context
            .RoomMembers.Where(member => member.RoomId == roomId)
            .Select(member => member.UserId)
            .ToListAsync();

        var profiles = await _context
            .Profiles.AsNoTracking()
            .Where(u => u.Username != null && u.Handle != null)
            .Where(u => EF.Functions.ILike(u.Handle!, $"{handle}%"))
            .Where(u => !currentMembersInRoom.Contains(u.Id))
            .Where(u => !existingMemberIds.Contains(u.Id))
            .Where(u => u.Id != callerId)
            .OrderBy(u => u.Handle)
            .Take(maxResults)
            .ToListAsync();

        return profiles.ConvertAll(_profileResponseMapper.ToUserSearchResponse);
    }

    public async Task SetMemberRoleAsync(Guid roomId, Guid userId, RoomRole role, Guid callerId)
    {
        if (role is not RoomRole.Editor and not RoomRole.Viewer)
        {
            throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "Members must be editors or viewers."
            );
        }

        var (_, editors) = GetRoomRolesOrThrow(roomId);
        RequireCanManageRoles(roomId, callerId, editors);

        if (userId == callerId && role == RoomRole.Editor)
        {
            throw new UnauthorizedAccessException(
                "Caller should not be able to make themself an editor"
            );
        }

        if (IsOwner(roomId, userId))
            throw new InvalidOperationException("The room owner's role cannot be changed.");

        var membership = await _context.RoomMembers.SingleOrDefaultAsync(member =>
            member.RoomId == roomId && member.UserId == userId
        );

        if (membership?.Role == role)
            return;

        if (membership is null)
        {
            if (!await _context.Profiles.AnyAsync(profile => profile.Id == userId))
                throw new NotFoundException($"User with id {userId} not found");

            _context.RoomMembers.Add(
                new RoomMember
                {
                    RoomId = roomId,
                    UserId = userId,
                    Role = role,
                }
            );
        }
        else
        {
            membership.Role = role;
        }

        await _context.SaveChangesAsync();

        if (!_roomTracker.SetMemberRole(roomId, userId, role))
            throw new InvalidOperationException("The member's cached role could not be updated.");
    }

    public async Task<IReadOnlyCollection<string>> RevokeMemberAccessAsync(
        Guid roomId,
        Guid userId,
        Guid callerId
    )
    {
        var (_, editors) = GetRoomRolesOrThrow(roomId);
        RequireCanManageRoles(roomId, callerId, editors);

        if (userId == callerId)
        {
            throw new UnauthorizedAccessException(
                "Use the leave-room flow to remove your own access."
            );
        }

        if (IsOwner(roomId, userId))
            throw new InvalidOperationException("The room owner's access cannot be revoked.");

        var membership = await _context.RoomMembers.SingleOrDefaultAsync(member =>
            member.RoomId == roomId && member.UserId == userId
        );

        if (membership is not null)
        {
            _context.RoomMembers.Remove(membership);
            await _context.SaveChangesAsync();
        }

        var removedConnections = _roomTracker.RemoveMemberRoleAndConnections(roomId, userId);
        if (removedConnections.Count > 0)
            await FinaliseRoomDepartureAsync(roomId);

        return removedConnections;
    }

    private (
        IReadOnlyCollection<Guid> viewers,
        IReadOnlyCollection<Guid> editors
    ) GetRoomRolesOrThrow(Guid roomId)
    {
        if (
            !_roomTracker.TryGetViewersInRoom(roomId, out var viewers)
            || !_roomTracker.TryGetEditorsInRoom(roomId, out var editors)
        )
        {
            throw new InvalidOperationException(
                $"Room {roomId} is not tracked. Ensure the room is initialised before doing operations to room."
            );
        }

        return (viewers, editors);
    }

    private Visibility GetRoomVisibilityOrThrow(Guid roomId)
    {
        if (!_roomTracker.TryGetVisibilityOfRoom(roomId, out var visibility))
        {
            throw new InvalidOperationException(
                $"Room {roomId} is not tracked. Ensure the room is initialised before doing operations to room."
            );
        }

        return visibility;
    }

    private void RequireCanManageRoles(
        Guid roomId,
        Guid callerId,
        IReadOnlyCollection<Guid> editors
    )
    {
        var hasAccess = IsOwner(roomId, callerId) || IsEditor(editors, callerId);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException(
                "User does not have permission to change other's roles."
            );
        }
    }

    private void RequireEditPermission(Guid roomId, Guid userId)
    {
        Visibility visibility = GetRoomVisibilityOrThrow(roomId);

        if (visibility == Visibility.PublicEdit)
            return;

        var (_, editors) = GetRoomRolesOrThrow(roomId);

        var hasAccess = IsOwner(roomId, userId) || IsEditor(editors, userId);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException(
                "User does not have permission to edit the room."
            );
        }
    }

    private void RequireViewPermission(Guid roomId, Guid userId)
    {
        Visibility visibility = GetRoomVisibilityOrThrow(roomId);

        if (visibility is Visibility.PublicView or Visibility.PublicEdit)
            return;

        var (viewers, editors) = GetRoomRolesOrThrow(roomId);

        var hasAccess =
            IsOwner(roomId, userId) || IsEditor(editors, userId) || viewers.Contains(userId);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException(
                "User does not have permission to view the room."
            );
        }
    }

    private bool IsOwner(Guid roomId, Guid userId) =>
        _roomTracker.GetTimetableById(roomId, roomId)?.UserId == userId;

    private static bool IsEditor(IReadOnlyCollection<Guid> editors, Guid userId) =>
        editors.Contains(userId);

    private async Task LoadRoomIntoTrackerAsync(Guid roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);

        if (room is null)
            throw new NotFoundException($"Room with roomId {roomId} not found");

        var timetables = await _context
            .Timetables.Where(t => t.RoomId == roomId)
            .ToListAsync()
            .MapAsync(list => list.Select(t => t.ToRoomTimetable()));

        var members = await _context.RoomMembers.Where(m => m.RoomId == roomId).ToListAsync();

        var editors = members.Where(m => m.Role == RoomRole.Editor).Select(m => m.UserId).ToList();
        var viewers = members.Where(m => m.Role == RoomRole.Viewer).Select(m => m.UserId).ToList();

        _roomTracker.SetRoom(
            roomId,
            new RoomInit(editors, viewers, [.. timetables], room.Visibility)
        );
    }
}

public enum CreateTimetableResult
{
    Success,
    RoomNotFound,
    TimetableIdConflict,
}
