using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class TimeTableController(AppDbContext _context, ITimeTableService service)
    : ControllerBase { }
