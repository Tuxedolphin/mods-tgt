using Backend.Models;
using Backend.Services.NusMods;
using Backend.Services.Optimiser;
using NSubstitute;
using Shouldly;

namespace Backend.Tests.Unit;

public class LessonCatalogueBuilderTests
{
    private static NusModsSession Session(string classNo, string type = "Tutorial") =>
        new(classNo, type, WeekDay.Monday, 600, 660, new HashSet<int> { 1, 2, 3 });

    private static TimetableModule Module(
        string code,
        string lessonNo,
        string type = "Tutorial"
    ) =>
        new()
        {
            ModuleCode = code,
            LessonNo = lessonNo,
            LessonType = type,
            Colour = "#ff0000",
        };

    private readonly INusModsClient _client = Substitute.For<INusModsClient>();

    private LessonCatalogueBuilder CreateBuilder() => new(_client);

    [Fact]
    public async Task BuildAsync_KnownModule_ReturnsAllOptionsWithCurrentChoice()
    {
        _client
            .GetModuleAsync("2026-2027", 1, "CS2100")
            .Returns(
                new ModuleTimetable("CS2100", [Session("T01"), Session("T02"), Session("T03")])
            );

        var (lessons, warnings) = await CreateBuilder()
            .BuildAsync(
                Guid.NewGuid(),
                [Module("CS2100", "T02")],
                "2026-2027",
                1,
                new HashSet<(string, string)>()
            );

        warnings.ShouldBeEmpty();
        var lesson = lessons.ShouldHaveSingleItem();
        lesson.CurrentClassNo.ShouldBe("T02");
        lesson.Locked.ShouldBeFalse();
        lesson.Options.Select(o => o.ClassNo).ShouldBe(["T01", "T02", "T03"], ignoreOrder: true);
    }

    [Fact]
    public async Task BuildAsync_ModuleNotOnNusMods_SkipsWithWarning()
    {
        _client.GetModuleAsync("2026-2027", 1, "NEW9999").Returns((ModuleTimetable?)null);

        var (lessons, warnings) = await CreateBuilder()
            .BuildAsync(
                Guid.NewGuid(),
                [Module("NEW9999", "T01")],
                "2026-2027",
                1,
                new HashSet<(string, string)>()
            );

        lessons.ShouldBeEmpty();
        warnings.ShouldHaveSingleItem().Code.ShouldBe("moduleNotFound");
    }

    [Fact]
    public async Task BuildAsync_StoredLessonNoMissing_SkipsWithWarning()
    {
        _client
            .GetModuleAsync("2026-2027", 1, "CS2100")
            .Returns(new ModuleTimetable("CS2100", [Session("T01")]));

        var (lessons, warnings) = await CreateBuilder()
            .BuildAsync(
                Guid.NewGuid(),
                [Module("CS2100", "T99")],
                "2026-2027",
                1,
                new HashSet<(string, string)>()
            );

        lessons.ShouldBeEmpty();
        warnings.ShouldHaveSingleItem().Code.ShouldBe("lessonNotFound");
    }

    [Fact]
    public async Task BuildAsync_LockOnValidLesson_MarksLocked()
    {
        _client
            .GetModuleAsync("2026-2027", 1, "CS2100")
            .Returns(new ModuleTimetable("CS2100", [Session("T01"), Session("T02")]));

        var (lessons, _) = await CreateBuilder()
            .BuildAsync(
                Guid.NewGuid(),
                [Module("CS2100", "T01")],
                "2026-2027",
                1,
                new HashSet<(string, string)> { ("CS2100", "Tutorial") }
            );

        lessons.ShouldHaveSingleItem().Locked.ShouldBeTrue();
    }

    [Fact]
    public async Task BuildAsync_GroupsSessionsByClassNoAndLessonType()
    {
        _client
            .GetModuleAsync("2026-2027", 1, "CS2100")
            .Returns(
                new ModuleTimetable(
                    "CS2100",
                    [Session("T01"), Session("T01"), Session("L1", "Lecture")]
                )
            );

        var (lessons, _) = await CreateBuilder()
            .BuildAsync(
                Guid.NewGuid(),
                [Module("CS2100", "T01"), Module("CS2100", "L1", "Lecture")],
                "2026-2027",
                1,
                new HashSet<(string, string)>()
            );

        lessons.Count.ShouldBe(2);
        lessons.Single(l => l.LessonType == "Tutorial").Options.Single().Sessions.Count.ShouldBe(2);
        lessons.Single(l => l.LessonType == "Lecture").Options.Count.ShouldBe(1);
    }
}
