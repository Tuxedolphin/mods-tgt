using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class ProfileController(IProfileService profileService) : BaseController
{
    private readonly IProfileService _profileService = profileService;

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var profile = await _profileService.GetCurrentUserProfileAsync(GetUserId());
        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] Profile request)
    {
        await _profileService.UpdateCurrentUserProfileAsync(GetUserId(), request);
        return NoContent();
    }
}
