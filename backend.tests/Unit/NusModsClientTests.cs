using System.Net;
using Backend.Exceptions;
using Backend.Services.NusMods;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;

namespace Backend.Tests.Unit;

public class NusModsClientTests
{
    private const string ModuleJson = """
    {
      "moduleCode": "CS2103T",
      "semesterData": [
        { "semester": 1, "timetable": [
            { "classNo": "G12", "lessonType": "Lecture", "day": "Thursday",
              "startTime": "0800", "endTime": "0900", "weeks": [1,2,3] } ] }
      ]
    }
    """;

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            CallCount++;
            return Task.FromResult(respond(request));
        }
    }

    private static NusModsClient CreateClient(StubHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.nusmods.com/v2/") },
            new MemoryCache(new MemoryCacheOptions { SizeLimit = 128 })
        );

    [Fact]
    public async Task GetModuleAsync_ValidResponse_ParsesModule()
    {
        var handler = new StubHandler(req =>
        {
            req.RequestUri!.ToString()
                .ShouldBe("https://api.nusmods.com/v2/2026-2027/modules/CS2103T.json");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ModuleJson),
            };
        });

        var result = await CreateClient(handler).GetModuleAsync("2026-2027", 1, "CS2103T");

        result.ShouldNotBeNull();
        result.Sessions.ShouldHaveSingleItem().ClassNo.ShouldBe("G12");
    }

    [Fact]
    public async Task GetModuleAsync_CalledTwice_UsesCache()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ModuleJson),
        });
        var client = CreateClient(handler);

        await client.GetModuleAsync("2026-2027", 1, "CS2103T");
        await client.GetModuleAsync("2026-2027", 1, "CS2103T");

        handler.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetModuleAsync_NotFound_ReturnsNull()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        (await CreateClient(handler).GetModuleAsync("2026-2027", 1, "ZZ9999")).ShouldBeNull();
    }

    [Fact]
    public async Task GetModuleAsync_ServerError_ThrowsExternalServiceException()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        await Should.ThrowAsync<ExternalServiceException>(
            () => CreateClient(handler).GetModuleAsync("2026-2027", 1, "CS2103T")
        );
    }
}
