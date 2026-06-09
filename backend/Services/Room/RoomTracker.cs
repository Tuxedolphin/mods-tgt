using System.Collections.Concurrent;
using Backend.DTOs;
using Backend.Infrastructure;

namespace Backend.Services.Room;

public class RoomTracker : IRoomTracker
{
    private static readonly ConcurrentDictionary<Guid, Guid> _userToRoomMap = new();
    private static readonly ConcurrentDictionary<Guid, ConcurrentHashSet<Guid>> _roomToUsersMap =
        new();

    public bool AddRoom(Guid roomId)
    {
        return _roomToUsersMap.TryAdd(roomId, []);
    }

    public bool AddUserToRoom(Guid userId, Guid roomId)
    {
        if (_roomToUsersMap.TryGetValue(roomId, out var hashSet))
        {
            hashSet.Add(userId);
            _userToRoomMap.AddOrUpdate(userId, roomId, (_, _) => roomId);
            return true;
        }

        return false;
    }

    public IReadOnlyCollection<RoomInformation> GetAllRoomInformation()
    {
        return
        [
            .. _roomToUsersMap.Select(kvp => new RoomInformation(
                RoomId: kvp.Key,
                Users: kvp.Value
            )),
        ];
    }

    public bool GetRoomOfUser(Guid userId, out Guid roomId)
    {
        return _userToRoomMap.TryGetValue(userId, out roomId);
    }

    public IReadOnlyCollection<Guid> GetUsersInRoom(Guid roomId)
    {
        return _roomToUsersMap.TryGetValue(roomId, out var usersMap) ? usersMap : [];
    }

    public bool RemoveUserFromRoom(Guid userId, Guid roomId)
    {
        if (_roomToUsersMap.TryGetValue(roomId, out var roomSet))
        {
            roomSet.Remove(userId);
            if (roomSet.Count == 0)
                _roomToUsersMap.TryRemove(roomId, out var _);

            return true;
        }

        // We don't silently fail here for logging purposes
        return false;
    }
}
