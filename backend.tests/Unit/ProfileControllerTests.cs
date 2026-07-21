using System.Security.Claims;
using Backend.Controllers;
using Backend.DTOs;
using Backend.Services.Profiles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace Backend.Tests.Unit;

public class ProfileControllerTests
{
    [Fact]
    public async Task CheckHandleAsync_ReturnsStructuredAvailabilityResponse()
    {
        var userId = Guid.NewGuid();
        var service = Substitute.For<IProfileService>();
        var expected = new HandleAvailabilityResponse(false, HandleUnavailableReason.Taken);
        service.CheckHandleAvailabilityAsync(userId, "existing-handle").Returns(expected);

        var controller = new ProfileController(service)
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

        var result = await controller.CheckHandleAsync("existing-handle");

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(expected);
        await service.Received(1).CheckHandleAvailabilityAsync(userId, "existing-handle");
    }
}
