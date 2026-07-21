using Backend.Models;

namespace Backend.Services.Rooms;

public interface IProfileTracker
{
    public void SetUsers(IReadOnlyCollection<Profile> Profiles);
    public IReadOnlyCollection<Profile> GetAllUsers();
    public bool TryGetUserById(Guid userId, out Profile? profile);
    public void SetUser(Profile profile);
    public bool RemoveUser(Guid userId);
    public bool RemoveUsers(IReadOnlyCollection<Guid> users);
    public void ClearAllUsers();
}
