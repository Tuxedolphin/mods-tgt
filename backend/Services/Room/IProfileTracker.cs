using Backend.Models;

namespace Backend.Services.Room;

public interface IProfileTracker
{
    public void SetUsers(IReadOnlyCollection<Profile> Profiles);
    public IReadOnlyCollection<Profile> GetAllUsers();
    public bool GetUserById(Guid userId, out Profile? profile);
    public void SetUser(Profile profile);
    public bool RemoveUser(Guid userId);
    public void ClearAllUsers();
}
