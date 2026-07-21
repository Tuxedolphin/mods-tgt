using System.Collections.Concurrent;
using Backend.Models;

namespace Backend.Services.Rooms;

public class ProfileTracker : IProfileTracker
{
    private readonly ConcurrentDictionary<Guid, Profile> _profileTracker = new();

    public void SetUser(Profile profile) => _profileTracker[profile.Id] = profile;

    public void ClearAllUsers() => _profileTracker.Clear();

    public IReadOnlyCollection<Profile> GetAllUsers() => [.. _profileTracker.Values];

    public bool TryGetUserById(Guid userId, out Profile? profile) =>
        _profileTracker.TryGetValue(userId, out profile);

    public bool RemoveUser(Guid userId) => _profileTracker.TryRemove(userId, out _);

    public bool RemoveUsers(IReadOnlyCollection<Guid> users) =>
        users.Select(RemoveUser).Aggregate(true, (possible, next) => next && possible);

    public void SetUsers(IReadOnlyCollection<Profile> profiles) =>
        profiles.ToList().ForEach(SetUser);
}
