using Backend.DTOs;
using Backend.Exceptions;
using Backend.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IAuthService authService) : BaseController
{
    private readonly IAuthService _authService = authService;

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> RegisterAsync(
        [FromBody] RegisterRequest request
    )
    {
        var response = await _authService.RegisterAsync(request);

        if (response.RequiresEmailConfirmation)
            return Accepted(new { message = response.Message });

        return StatusCode(201, response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> LoginAsync([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshTokenAsync(
        [FromBody] RefreshTokenRequest request
    )
    {
        var response = await _authService.RefreshTokenAsync(request);
        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> LogoutAsync()
    {
        await _authService.LogoutAsync(GetBearerToken());
        return NoContent();
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAllAsync()
    {
        await _authService.LogoutAllAccountsAsync(GetBearerToken());
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request
    )
    {
        // Exceptions were swallowed here. Returns the exception to client-side to show
        // user.
        await _authService.ForgotPasswordAsync(request);
        return Ok();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return NoContent();
    }

    [HttpPost("update-password")]
    [Authorize]
    public async Task<IActionResult> UpdatePasswordAsync([FromBody] UpdatePasswordRequest request)
    {
        await _authService.UpdatePasswordAsync(request, GetBearerToken());
        return NoContent();
    }

    private string GetBearerToken()
    {
        var header = HttpContext.Request.Headers.Authorization.ToString();

        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : throw new UnauthorizedAccessException("Bearer token not found in request.");
    }
}
