using System.Security.Claims;
using Backend.Controllers;
using Backend.DTOs;
using Backend.Services.Timetables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace Backend.Tests.Unit;

public class TimetableControllerTests
{
    [Fact]
    public async Task GetSharedTimetablesAsync_ReturnsAuthenticatedUsersSharedTimetables()
    {
        var userId = Guid.NewGuid();
        var service = Substitute.For<ITimetableService>();
        var expected = new List<TimetableSummaryResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Shared timetable",
                Semester = 1,
                AcademicYear = "2026-2027",
                CreatedAt = DateTime.UtcNow,
            },
        };
        service.GetSharedTimetablesAsync(userId).Returns(expected);

        var controller = new TimetableController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                            "test"
                        )
                    ),
                },
            },
        };

        var result = await controller.GetSharedTimetablesAsync();

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBeSameAs(expected);
        await service.Received(1).GetSharedTimetablesAsync(userId);
    }
}
