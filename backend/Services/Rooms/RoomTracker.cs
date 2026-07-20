using System.Collections.Concurrent;
using Backend.DTOs;
using Backend.Exceptions;
using Backend.Infrastructure;
using Backend.Models;

namespace Backend.Services.Rooms;

public class RoomTracker : IRoomTracker
{
    private record ConnectionSession(Guid UserId, Guid? RoomId);

    private record RoomState
    {
        public ConcurrentHashSet<Guid> Editors { get; init; } = [];

        public ConcurrentHashSet<Guid> Viewers { get; init; } = [];
        public ConcurrentDictionary<Guid, RoomTimetable> Timetables { get; init; } = new();

        public Visibility Visibility = Visibility.Restricted;
    }

    private readonly ConcurrentDictionary<string, ConnectionSession> _connections = new();

    private readonly ConcurrentDictionary<Guid, RoomState> _rooms = new();

    private readonly ConcurrentHashSet<Guid> _changedTimetables = [];
    private readonly ConcurrentDictionary<Guid, ConcurrentHashSet<Guid>> _deletedTimetables = [];

    public bool AddRoom(Guid roomId) => _rooms.TryAdd(roomId, new RoomState());

    public bool RoomExists(Guid roomId) => _rooms.ContainsKey(roomId);

    public bool RegisterConnection(string connectionId, Guid userId) =>
        _connections.TryAdd(connectionId, new ConnectionSession(userId, null));

    public RoomConnectionMove MoveConnectionToRoom(string connectionId, Guid userId, Guid roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var roomState))
            throw new InvalidOperationException($"Room {roomId} is not tracked.");

        lock (roomState)
        {
            if (
                !_rooms.TryGetValue(roomId, out var currentRoomState)
                || !ReferenceEquals(roomState, currentRoomState)
            )
            {
                throw new InvalidOperationException($"Room {roomId} is no longer tracked.");
            }

            if (!CanViewRoom(roomId, roomState, userId))
                throw new ForbiddenException("User does not have permission to enter the room");

            while (_connections.TryGetValue(connectionId, out var currentConnection))
            {
                if (currentConnection.UserId != userId)
                {
                    throw new UnauthorizedAccessException(
                        $"Connection {connectionId} does not belong to user {userId}."
                    );
                }

                if (currentConnection.RoomId == roomId)
                    return new RoomConnectionMove(roomId);

                var updatedConnection = currentConnection with { RoomId = roomId };
                if (_connections.TryUpdate(connectionId, updatedConnection, currentConnection))
                    return new RoomConnectionMove(currentConnection.RoomId);
            }

            throw new InvalidOperationException($"Connection {connectionId} is not registered.");
        }
    }

    public RoomConnectionDeparture? LeaveConnectionFromRoom(string connectionId, Guid roomId)
    {
        if (!_connections.TryGetValue(connectionId, out var initialConnection))
            return null;

        if (initialConnection.RoomId != roomId)
            return null;

        if (!_rooms.TryGetValue(roomId, out var roomState))
            return LeaveUntrackedRoom(connectionId, roomId);

        lock (roomState)
        {
            while (_connections.TryGetValue(connectionId, out var currentConnection))
            {
                if (currentConnection.RoomId != roomId)
                    return null;

                var updatedConnection = currentConnection with { RoomId = null };
                if (_connections.TryUpdate(connectionId, updatedConnection, currentConnection))
                {
                    return new RoomConnectionDeparture(currentConnection.UserId);
                }
            }

            return null;
        }
    }

    public ConnectionRemoval? RemoveConnection(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var removedConnection))
            return null;

        return new ConnectionRemoval(
            removedConnection.UserId,
            removedConnection.RoomId,
            !HasConnections(removedConnection.UserId)
        );
    }

    public bool TryGetRoomOfConnection(string connectionId, out Guid roomId)
    {
        if (
            _connections.TryGetValue(connectionId, out var connection)
            && connection.RoomId is { } currentRoomId
        )
        {
            roomId = currentRoomId;
            return true;
        }

        roomId = default;
        return false;
    }

    public bool IsConnectionInRoom(string connectionId, Guid roomId) =>
        _connections.TryGetValue(connectionId, out var connection) && connection.RoomId == roomId;

    private bool HasConnections(Guid userId) =>
        _connections.Values.Any(connection => connection.UserId == userId);

    public bool TryGetUsersInRoom(Guid roomId, out IReadOnlyCollection<Guid> users)
    {
        if (!_rooms.ContainsKey(roomId))
        {
            users = [];
            return false;
        }

        users =
        [
            .. _connections
                .Values.Where(connection => connection.RoomId == roomId)
                .Select(connection => connection.UserId)
                .Distinct(),
        ];

        return true;
    }

    private RoomConnectionDeparture? LeaveUntrackedRoom(string connectionId, Guid roomId)
    {
        while (_connections.TryGetValue(connectionId, out var currentConnection))
        {
            if (currentConnection.RoomId != roomId)
                return null;

            var updatedConnection = currentConnection with { RoomId = null };
            if (_connections.TryUpdate(connectionId, updatedConnection, currentConnection))
            {
                return new RoomConnectionDeparture(currentConnection.UserId);
            }
        }

        return null;
    }

    private bool HasConnectionsInRoom(Guid roomId) =>
        _connections.Values.Any(connection => connection.RoomId == roomId);

    private static bool CanViewRoom(Guid roomId, RoomState roomState, Guid userId)
    {
        return roomState.Visibility is Visibility.PublicView or Visibility.PublicEdit
            || roomState.Editors.Contains(userId)
            || roomState.Viewers.Contains(userId)
            || roomState.Timetables.GetValueOrDefault(roomId)?.UserId == userId;
    }

    public bool TryGetTimetablesInRoom(
        Guid roomId,
        out IReadOnlyCollection<RoomTimetable> timetables
    )
    {
        var found = _rooms.TryGetValue(roomId, out var roomState);
        timetables = found ? [.. roomState!.Timetables.Values] : [];

        return found;
    }

    public bool SetMemberRole(Guid roomId, Guid userId, RoomRole role)
    {
        if (!_rooms.TryGetValue(roomId, out var roomState))
            return false;

        if (role is not RoomRole.Editor and not RoomRole.Viewer)
        {
            throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "Members must be editors or viewers."
            );
        }

        lock (roomState)
        {
            if (
                !_rooms.TryGetValue(roomId, out var currentRoomState)
                || !ReferenceEquals(roomState, currentRoomState)
            )
            {
                return false;
            }

            roomState.Editors.Remove(userId);
            roomState.Viewers.Remove(userId);

            if (role == RoomRole.Editor)
                roomState.Editors.Add(userId);
            else
                roomState.Viewers.Add(userId);

            return true;
        }
    }

    public IReadOnlyCollection<string> RemoveMemberRoleAndConnections(Guid roomId, Guid userId)
    {
        if (!_rooms.TryGetValue(roomId, out var roomState))
            return [];

        lock (roomState)
        {
            roomState.Editors.Remove(userId);
            roomState.Viewers.Remove(userId);

            var removedConnections = new List<string>();
            foreach (var entry in _connections)
            {
                var currentConnection = entry.Value;
                if (currentConnection.UserId != userId || currentConnection.RoomId != roomId)
                    continue;

                while (
                    _connections.TryGetValue(entry.Key, out currentConnection)
                    && currentConnection.UserId == userId
                    && currentConnection.RoomId == roomId
                )
                {
                    var updatedConnection = currentConnection with { RoomId = null };
                    if (_connections.TryUpdate(entry.Key, updatedConnection, currentConnection))
                    {
                        removedConnections.Add(entry.Key);
                        break;
                    }
                }
            }

            return removedConnections;
        }
    }

    public bool TryGetEditorsInRoom(Guid roomId, out IReadOnlyCollection<Guid> editors)
    {
        var found = _rooms.TryGetValue(roomId, out var roomState);
        editors = found ? roomState!.Editors.ToArray() : [];

        return found;
    }

    public bool TryGetViewersInRoom(Guid roomId, out IReadOnlyCollection<Guid> viewers)
    {
        var found = _rooms.TryGetValue(roomId, out var roomState);
        viewers = found ? roomState!.Viewers.ToArray() : [];

        return found;
    }

    public bool TryGetVisibilityOfRoom(Guid roomId, out Visibility visibility)
    {
        var found = _rooms.TryGetValue(roomId, out var roomState);
        visibility = found ? roomState!.Visibility : Visibility.Restricted;

        return found;
    }

    public bool UpdateRoomVisibility(
        Guid roomId,
        Visibility visibility,
        out IReadOnlyCollection<string> removedConnectionIds
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

        removedConnectionIds = [];
        if (!_rooms.TryGetValue(roomId, out var roomState))
            return false;

        lock (roomState)
        {
            if (
                !_rooms.TryGetValue(roomId, out var currentRoomState)
                || !ReferenceEquals(roomState, currentRoomState)
            )
            {
                return false;
            }

            roomState.Visibility = visibility;
            if (visibility != Visibility.Restricted)
                return true;

            var removedConnections = new List<string>();
            foreach (var entry in _connections)
            {
                var currentConnection = entry.Value;
                if (
                    currentConnection.RoomId != roomId
                    || CanViewRoom(roomId, roomState, currentConnection.UserId)
                )
                {
                    continue;
                }

                while (
                    _connections.TryGetValue(entry.Key, out currentConnection)
                    && currentConnection.RoomId == roomId
                    && !CanViewRoom(roomId, roomState, currentConnection.UserId)
                )
                {
                    var updatedConnection = currentConnection with { RoomId = null };
                    if (_connections.TryUpdate(entry.Key, updatedConnection, currentConnection))
                    {
                        removedConnections.Add(entry.Key);
                        break;
                    }
                }
            }

            removedConnectionIds = removedConnections;
            return true;
        }
    }

    // This assumes that there is something to be changed about the timetable,
    // be it to create it in the database or to update the entry in the database
    public bool AddOrUpdateTimetable(RoomTimetable timetable)
    {
        var roomId = timetable.RoomId;

        if (_rooms.TryGetValue(roomId, out var roomState))
        {
            roomState.Timetables[timetable.Id] = timetable;
            _changedTimetables.Add(timetable.Id);
            return true;
        }

        return false;
    }

    public bool DeleteTimetable(Guid roomId, Guid timetableId)
    {
        if (_rooms.TryGetValue(roomId, out var roomState))
        {
            roomState.Timetables.TryRemove(timetableId, out _);
            _changedTimetables.Add(timetableId);
            _deletedTimetables.GetOrAdd(roomId, []).Add(timetableId);
            return true;
        }

        return false;
    }

    public bool SetRoom(Guid roomId, RoomInit init)
    {
        var invalidTimetable = init.Timetables.FirstOrDefault(t => t.RoomId != roomId);
        if (invalidTimetable is not null)
        {
            throw new ArgumentException(
                $"Timetable {invalidTimetable.Id} does not belong to room {roomId}.",
                nameof(init)
            );
        }

        var roomState = new RoomState
        {
            Editors = [.. init.Editors],
            Viewers = [.. init.Viewers],
            Timetables = new ConcurrentDictionary<Guid, RoomTimetable>(
                init.Timetables.ToDictionary(t => t.Id, t => t)
            ),
            Visibility = init.Visibility,
        };

        return _rooms.TryAdd(roomId, roomState);
    }

    public bool CloseRoom(Guid roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var roomState))
            return false;

        lock (roomState)
        {
            if (HasConnectionsInRoom(roomId))
                return false;

            return _rooms.TryRemove(roomId, out _);
        }
    }

    public RoomTimetable? GetTimetableById(Guid roomId, Guid timetableId) =>
        _rooms.TryGetValue(roomId, out var roomInfo)
            ? roomInfo.Timetables.GetValueOrDefault(timetableId)
            : null;

    public bool TryGetChangedTimetables(
        Guid roomId,
        out IReadOnlyCollection<RoomTimetable> timetables
    )
    {
        if (!TryGetTimetablesInRoom(roomId, out timetables))
            return false;

        timetables = [.. timetables.Where(t => _changedTimetables.Contains(t.Id))];
        return true;
    }

    public IReadOnlyCollection<Guid> GetDeletedTimetables(Guid roomId) =>
        _deletedTimetables.TryGetValue(roomId, out var timetableIds) ? [.. timetableIds] : [];

    public IReadOnlyCollection<Guid> RemoveTimetablesFromChanged(
        IReadOnlyCollection<Guid> timetables
    ) => [.. timetables.Where(id => !_changedTimetables.Remove(id))];

    public IReadOnlyCollection<Guid> RemoveTimetablesFromDeleted(
        Guid roomId,
        IReadOnlyCollection<Guid> timetables
    ) =>
        [
            .. timetables.Where(id =>
                !(_deletedTimetables.TryGetValue(roomId, out var hash) && hash.Remove(id))
            ),
        ];
}
