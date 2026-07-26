using Backend.Models;
using Backend.Services.NusMods;

namespace Backend.Services.Optimiser;

public class LessonCatalogueBuilder(INusModsClient client)
{
    public async Task<(
        IReadOnlyList<LessonInput> Lessons,
        IReadOnlyList<SolveWarning> Warnings
    )> BuildAsync(
        Guid userId,
        IEnumerable<TimetableModule> metaData,
        string academicYear,
        int semester,
        IReadOnlySet<(string ModuleCode, string LessonType)> locks
    )
    {
        var lessons = new List<LessonInput>();
        var warnings = new List<SolveWarning>();

        foreach (var stored in metaData)
        {
            var module = await client.GetModuleAsync(academicYear, semester, stored.ModuleCode);
            if (module is null)
            {
                warnings.Add(
                    new SolveWarning("moduleNotFound", userId, stored.ModuleCode, null, null)
                );
                continue;
            }

            var options = module
                .Sessions.Where(s => s.LessonType == stored.LessonType)
                .GroupBy(s => s.ClassNo)
                .Select(g => new LessonOption(g.Key, [.. g]))
                .ToList();

            if (!options.Any(o => o.ClassNo == stored.LessonNo))
            {
                warnings.Add(
                    new SolveWarning(
                        "lessonNotFound",
                        userId,
                        stored.ModuleCode,
                        stored.LessonType,
                        $"Class {stored.LessonNo} no longer offered"
                    )
                );
                continue;
            }

            var locked = locks.Contains((stored.ModuleCode, stored.LessonType));
            lessons.Add(
                new LessonInput(
                    stored.ModuleCode,
                    stored.LessonType,
                    stored.LessonNo,
                    locked,
                    options
                )
            );
        }

        return (lessons, warnings);
    }
}
