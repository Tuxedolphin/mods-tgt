using Backend.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

public class BaseController : ControllerBase
{
    /// <summary>
    /// Retrieves the user ID from the JWT token claims.
    /// </summary>
    /// <returns>The user ID as a Guid.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user ID is not found in the token.</exception>
    protected Guid GetUserId() => ClaimsHelper.GetUserId(User);
}
