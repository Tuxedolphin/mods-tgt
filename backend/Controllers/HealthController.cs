using Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController(AppDbContext _context) : BaseController
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        try
        {
            var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();
            await conn.CloseAsync();
            return Ok(new { status = "Healthy", database = "Connected" });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "Unhealthy",
                database = "Not Connected",
                error = ex.Message,
                type = ex.GetType().Name,
                connectionString = _context.Database.GetConnectionString()
            });
        }
    }
}
