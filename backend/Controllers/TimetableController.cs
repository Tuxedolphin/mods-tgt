using Backend.DTOs;
using Backend.Services.Timetables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TimetableController(ITimetableService service) : BaseController
{
    private readonly ITimetableService _service = service;

    [HttpPost]
    public async Task<ActionResult<TimetableResponse>> CreateTimetable(
        [FromBody] CreateTimetableRequest request
    )
    {
        TimetableResponse timetable = await _service.CreateTimetableAsync(request, GetUserId());

        return CreatedAtRoute(nameof(GetTimetableByIdAsync), new { id = timetable.Id }, timetable);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTimetableAsync([FromRoute] Guid id)
    {
        await _service.DeleteTimetableAsync(id, GetUserId());

        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<List<TimetableSummaryResponse>>> GetTimetables()
    {
        var timetables = await _service.GetTimetablesAsync(GetUserId());
        return Ok(timetables);
    }

    [HttpGet("{id}", Name = nameof(GetTimetableByIdAsync))]
    public async Task<ActionResult<TimetableResponse>> GetTimetableByIdAsync([FromRoute] Guid id)
    {
        var timetable = await _service.GetTimetableByIdAsync(id, GetUserId());
        return Ok(timetable);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTimetableAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateTimetableRequest request
    )
    {
        await _service.UpdateTimetableAsync(id, request, GetUserId());
        return NoContent();
    }

    [HttpGet("shared")]
    public async Task<ActionResult<List<TimetableSummaryResponse>>> GetSharedTimetablesAsync()
    {
        var timetables = await _service.GetSharedTimetablesAsync(GetUserId());

        return Ok(timetables);
    }
}
