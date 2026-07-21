using Microsoft.EntityFrameworkCore;

namespace Backend.Models;

[Index(nameof(Handle), IsUnique = true)]
public class Profile
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string? Handle { get; set; }

    public string? Colour { get; set; }
    public string? DefaultTheme { get; set; }

    // We only store this as the path of avatar is deterministic
    // i.e. it is always {UserId}/avatar.webp
    public DateTime? AvatarUpdatedAt { get; set; }
}
