using System.Net;
using System.Text.Json;
using Backend.Exceptions;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services.NusMods;

public class NusModsClient(HttpClient httpClient, IMemoryCache cache) : INusModsClient
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
    private static readonly SemaphoreSlim CacheGate = new(1, 1);

    public async Task<ModuleTimetable?> GetModuleAsync(
        string academicYear,
        int semester,
        string moduleCode,
        CancellationToken ct = default
    )
    {
        var cacheKey = $"nusmods:{academicYear}:{semester}:{moduleCode}";

        if (cache.TryGetValue(cacheKey, out Task<ModuleTimetable?>? cached) && cached is not null)
            return await cached;

        // Concurrent solves routinely ask for the same module, so only the first
        // caller fetches and the rest await that same task.
        await CacheGate.WaitAsync(ct);
        Task<ModuleTimetable?> fetch;
        try
        {
            if (cache.TryGetValue(cacheKey, out cached) && cached is not null)
                fetch = cached;
            else
            {
                fetch = FetchModuleAsync(academicYear, semester, moduleCode, ct);
                _ = cache.Set(
                    cacheKey,
                    fetch,
                    new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl }
                        .SetSize(1)
                );
            }
        }
        finally
        {
            CacheGate.Release();
        }

        try
        {
            return await fetch;
        }
        catch
        {
            // A failed fetch must not be served to later callers.
            cache.Remove(cacheKey);
            throw;
        }
    }

    private async Task<ModuleTimetable?> FetchModuleAsync(
        string academicYear,
        int semester,
        string moduleCode,
        CancellationToken ct
    )
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(
                $"{academicYear}/modules/{moduleCode}.json",
                HttpCompletionOption.ResponseHeadersRead,
                ct
            );
        }
        catch (HttpRequestException e)
        {
            throw new ExternalServiceException($"NUSMods request failed for {moduleCode}", e);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException(
                $"NUSMods returned {(int)response.StatusCode} for {moduleCode}"
            );

        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            return NusModsParsing.ParseModule(moduleCode, semester, doc);
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
