using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class TimeTableController(ITimeTableService service) : BaseController
{
    private readonly ITimeTableService _service = service;

    [HttpPost]
    public async Task<IActionResult> CreateTimeTable([FromBody] CreateTimeTableRequest request)
    {
        TimeTable timeTable = await _service.CreateTimeTableAsync(request, GetUserId());

        return CreatedAtAction(nameof(GetTimeTableById), new { id = timeTable.Id }, timeTable);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTimeTable([FromRoute] Guid id)
    {
        await _service.DeleteTimeTableAsync(id, GetUserId());
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<TimeTable>> GetTimeTables()
    {
        var timeTables = await _service.GetTimeTablesAsync(GetUserId());
        return Ok(timeTables);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TimeTable>> GetTimeTableById([FromRoute] Guid id)
    {
        var timeTable = await _service.GetTimeTableByIdAsync(id, GetUserId());
        return Ok(timeTable);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTimeTable(
        [FromRoute] Guid id,
        [FromBody] UpdateTimeTableRequest request
    )
    {
        await _service.UpdateTimeTableAsync(id, request, GetUserId());
        return NoContent();
    }
}
