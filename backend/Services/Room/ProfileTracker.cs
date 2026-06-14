using System.Collections.Concurrent;
using Backend.Models;

namespace Backend.Services.Room;

public class ProfileTracker : IProfileTracker
{
    private readonly ConcurrentDictionary<Guid, Profile> _profileTracker = new();

    public void SetUser(Profile profile)
    {
        _profileTracker[profile.Id] = profile;
    }

    public void ClearAllUsers()
    {
        _profileTracker.Clear();
    }

    public IReadOnlyCollection<Profile> GetAllUsers()
    {
        return [.. _profileTracker.Values];
    }

    public bool GetUserById(Guid userId, out Profile? profile)
    {
        return _profileTracker.TryGetValue(userId, out profile);
    }

    public bool RemoveUser(Guid userId)
    {
        return _profileTracker.TryRemove(userId, out _);
    }

    public void SetUsers(IReadOnlyCollection<Profile> profiles)
    {
        profiles.ToList().ForEach(SetUser);
    }
}
