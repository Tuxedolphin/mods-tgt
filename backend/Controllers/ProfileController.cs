using Backend.DTOs;
using Backend.Services.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ProfileController(IProfileService profileService) : BaseController
{
    private readonly IProfileService _profileService = profileService;

    [HttpGet("me")]
    public async Task<ActionResult<ProfileResponse>> GetUserAsync()
    {
        var profile = await _profileService.GetUserProfileAsync(GetUserId());
        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateUserAsync([FromBody] UpdateProfileRequest request)
    {
        await _profileService.UpdateUserProfileAsync(GetUserId(), request);
        return NoContent();
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteUserAsync()
    {
        await _profileService.DeleteUserProfileAsync(GetUserId());

        return NoContent();
    }

    [HttpPut("me/customisation")]
    public async Task<IActionResult> UpdateUserCustomisation(
        [FromBody] UpdateProfileCustomisationRequest request
    )
    {
        await _profileService.UpdateUserCustomisation(GetUserId(), request);

        return NoContent();
    }

    [HttpGet("check-handle")]
    [EnableRateLimiting("handle-check")]
    public async Task<ActionResult<HandleAvailabilityResponse>> CheckHandleAsync(
        [FromQuery] string handle
    )
    {
        var result = await _profileService.CheckHandleAvailabilityAsync(GetUserId(), handle);

        Response.Headers.CacheControl = "no-store";
        return Ok(result);
    }

    [HttpPut("avatar")]
    public async Task<ActionResult<ProfileResponse>> UpsertUserAvatarAsync(
        [FromForm] IFormFile file
    )
    {
        ProfileResponse response = await _profileService.UpsertUserAvatarAsync(
            GetUserId(),
            file.OpenReadStream()
        );

        return Ok(response);
    }

    [HttpDelete("avatar")]
    public async Task<IActionResult> DeleteUserAvatarAsync()
    {
        await _profileService.DeleteUserAvatarAsync(GetUserId());

        return NoContent();
    }
}
