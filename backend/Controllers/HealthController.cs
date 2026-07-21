using Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController(AppDbContext _context) : BaseController
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAsync()
    {
        try
        {
            var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();
            await conn.CloseAsync();
            return Ok(new { status = "Healthy!", database = "Connected" });
        }
        catch (Exception)
        {
            return StatusCode(503, new { status = "Unhealthy", database = "Not Connected" });
        }
    }
}
