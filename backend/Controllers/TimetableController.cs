using Backend.DTOs;
using Backend.Models;
using Backend.Services.Timetables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class TimetableController(ITimetableService service) : BaseController
{
    private readonly ITimetableService _service = service;

    [HttpPost]
    public async Task<IActionResult> CreateTimetable([FromBody] CreateTimetableRequest request)
    {
        Timetable timetable = await _service.CreateTimetableAsync(request, GetUserId());

        return CreatedAtAction(nameof(GetTimetableById), new { id = timetable.Id }, timetable);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTimetable([FromRoute] Guid id)
    {
        await _service.DeleteTimetableAsync(id, GetUserId());
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<TimetableSummaryResponse>> GetTimetables()
    {
        var timetables = await _service.GetTimetablesAsync(GetUserId());
        return Ok(timetables);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Timetable>> GetTimetableById([FromRoute] Guid id)
    {
        var timetable = await _service.GetTimetableByIdAsync(id, GetUserId());
        return Ok(timetable);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTimetable(
        [FromRoute] Guid id,
        [FromBody] UpdateTimetableRequest request
    )
    {
        await _service.UpdateTimetableAsync(id, request, GetUserId());
        return NoContent();
    }
}
