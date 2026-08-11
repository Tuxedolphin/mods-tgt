namespace Backend.Services.NusMods;

public interface INusModsClient
{
    Task<ModuleTimetable?> GetModuleAsync(
        string academicYear,
        int semester,
        string moduleCode,
        CancellationToken ct = default
    );
}
