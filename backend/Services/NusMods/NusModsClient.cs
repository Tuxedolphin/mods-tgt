using System.Net;
using System.Text.Json;
using Backend.Exceptions;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services.NusMods;

public class NusModsClient(HttpClient httpClient, IMemoryCache cache) : INusModsClient
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public async Task<ModuleTimetable?> GetModuleAsync(
        string academicYear,
        int semester,
        string moduleCode,
        CancellationToken ct = default
    )
    {
        var cacheKey = $"nusmods:{academicYear}:{semester}:{moduleCode}";
        if (cache.TryGetValue(cacheKey, out ModuleTimetable? cached))
            return cached;

        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync($"{academicYear}/modules/{moduleCode}.json", ct);
        }
        catch (HttpRequestException e)
        {
            throw new ExternalServiceException($"NUSMods request failed for {moduleCode}", e);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            cache.Set(cacheKey, (ModuleTimetable?)null, CacheTtl);
            return null;
        }

        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException(
                $"NUSMods returned {(int)response.StatusCode} for {moduleCode}"
            );

        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var module = NusModsParsing.ParseModule(moduleCode, semester, doc);
            cache.Set(cacheKey, module, CacheTtl);
            return module;
        }
        catch (JsonException e)
        {
            throw new ExternalServiceException(
                $"NUSMods returned malformed data for {moduleCode}",
                e
            );
        }
    }
}
