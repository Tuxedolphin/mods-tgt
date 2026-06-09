using System.Security.Claims;

namespace Backend.Infrastructure;

public static class ClaimsHelper
{
    public static Guid GetUserId(ClaimsPrincipal User)
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");

        return Guid.Parse(userId);
    }
}
