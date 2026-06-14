using System.Collections.Concurrent;
using Backend.Infrastructure;
using Backend.Models;

namespace Backend.Services.Room;

public class RoomTracker : IRoomTracker
{
    private record RoomState
    {
        public ConcurrentHashSet<Guid> Users { get; init; } = [];
        public ConcurrentDictionary<Guid, Timetable> Timetables { get; init; } = new();
    }

    private readonly ConcurrentDictionary<Guid, Guid> _userToRoomMap = new();

    // We maintain both room to users and room to timetables even though time table as userId
    // as not every user may have a timetable
    private readonly ConcurrentDictionary<Guid, RoomState> _rooms = new();

    private readonly ConcurrentHashSet<Guid> _changedTimetables = [];

    public bool AddRoom(Guid roomId)
    {
        return _rooms.TryAdd(roomId, new RoomState());
    }

    public bool AddUserToRoom(Guid userId, Guid roomId)
    {
        if (_rooms.TryGetValue(roomId, out var roomState))
        {
            // TODO: maybe add checks to make sure user is not in another room

            roomState.Users.Add(userId);

            _userToRoomMap[userId] = roomId;
            return true;
        }

        return false;
    }

    public bool GetRoomOfUser(Guid userId, out Guid roomId)
    {
        return _userToRoomMap.TryGetValue(userId, out roomId);
    }

    public bool GetTimetablesInRoom(Guid roomId, out IReadOnlyCollection<Timetable> timetables)
    {
        var found = _rooms.TryGetValue(roomId, out var roomState);
        timetables = found ? [.. roomState!.Timetables.Values] : [];

        return found;
    }

    public bool GetUsersInRoom(Guid roomId, out IReadOnlyCollection<Guid> users)
    {
        var found = _rooms.TryGetValue(roomId, out var roomState);
        users = found ? roomState!.Users.ToArray() : [];

        return found;
    }

    public bool RemoveUserFromRoom(Guid userId, Guid roomId)
    {
        if (_rooms.TryGetValue(roomId, out var roomState))
        {
            roomState.Users.Remove(userId);
            if (roomState.Users.Count == 0)
                DeleteRoom(roomId);

            // We don't remove the timetable from here as the timetable should be independent
            // of whether or not the user is in the room

            return true;
        }

        // We don't silently fail here for logging purposes
        return false;
    }

    public bool UpdateTimetable(Guid roomId, Timetable timetable)
    {
        if (_rooms.TryGetValue(roomId, out var roomState))
        {
            roomState.Timetables[timetable.Id] = timetable;
            return true;
        }

        return false;
    }

    public bool DeleteTimetable(Guid roomId, Guid timetableId)
    {
        if (_rooms.TryGetValue(roomId, out var roomState))
        {
            roomState.Timetables.TryRemove(timetableId, out _);
            return true;
        }

        return false;
    }

    public bool SetRoom(
        Guid roomId,
        IReadOnlyCollection<Guid> users,
        IReadOnlyCollection<Timetable> timetables
    )
    {
        var roomState = new RoomState
        {
            Users = [.. users],
            Timetables = new ConcurrentDictionary<Guid, Timetable>(
                timetables.ToDictionary(t => t.Id, t => t)
            ),
        };

        return _rooms.TryAdd(roomId, roomState);
    }

    public bool DeleteRoom(Guid roomId)
    {
        bool success = _rooms.TryRemove(roomId, out RoomState? roomState);

        if (roomState?.Users.IsEmpty == false)
            roomState.Users.ToList().ForEach(userId => _userToRoomMap.TryRemove(userId, out _));

        if (roomState?.Timetables.IsEmpty == false)
        {
            roomState
                .Timetables.Keys.ToList()
                .ForEach(timetableId => _changedTimetables.Remove(timetableId));
        }

        // We don't silently fail, even though the behaviour is the same, for logging purposes
        return success;
    }
}
