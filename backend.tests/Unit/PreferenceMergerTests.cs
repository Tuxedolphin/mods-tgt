using Backend.Models;
using Backend.Services.NusMods;
using Backend.Services.Optimiser;
using Shouldly;

namespace Backend.Tests.Unit;

public class PreferenceMergerTests
{
    [Fact]
    public void Merge_BothNull_ReturnsBaselineDefaultsAndFlagsIt()
    {
        var result = PreferenceMerger.Merge(null, null);

        result.UsedDefaults.ShouldBeTrue();
        result.LunchBreak.ShouldBe(Tier.NiceToHave);
        result.EarliestStartMin.ShouldBe(540);
        result.BlockedDays.ShouldBeEmpty();
        result.CompactDays.ShouldBe(Tier.Off);
    }

    [Fact]
    public void Merge_RoomOffBeatsGlobalImportant()
    {
        var global = new PreferencePayload { LunchBreak = Tier.Important };
        var room = new PreferencePayload { LunchBreak = Tier.Off };

        PreferenceMerger.Merge(global, room).LunchBreak.ShouldBe(Tier.Off);
    }

    [Fact]
    public void Merge_RoomUnsetInheritsGlobal()
    {
        var global = new PreferencePayload { CompactDays = Tier.Important };
        var room = new PreferencePayload();

        PreferenceMerger.Merge(global, room).CompactDays.ShouldBe(Tier.Important);
    }

    [Fact]
    public void Merge_LockedLessonsOnlyReadFromRoomRow()
    {
        var global = new PreferencePayload
        {
            LockedLessons = [new LockedLesson { ModuleCode = "CS2100", LessonType = "Tutorial" }],
        };

        PreferenceMerger.Merge(global, null).LockedLessons.ShouldBeEmpty();
    }

    [Fact]
    public void Merge_AnyRowPresent_NotFlaggedAsDefaults()
    {
        PreferenceMerger.Merge(new PreferencePayload(), null).UsedDefaults.ShouldBeFalse();
    }

    [Fact]
    public void Merge_TimesParsedToMinutes()
    {
        var global = new PreferencePayload
        {
            EarliestStart = "1000",
            PreferredWindow = new TimeWindow { Start = "1200", End = "1800" },
        };

        var result = PreferenceMerger.Merge(global, null);
        result.EarliestStartMin.ShouldBe(600);
        result.PreferredWindow.ShouldBe((720, 1080));
    }
}
