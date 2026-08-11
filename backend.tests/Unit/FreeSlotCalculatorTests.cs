using Backend.DTOs;
using Backend.Models;
using Backend.Services.NusMods;
using Backend.Services.Optimiser;
using Shouldly;

namespace Backend.Tests.Unit;

public class FreeSlotCalculatorTests
{
    private static readonly IReadOnlySet<int> AllWeeks = new HashSet<int>(Enumerable.Range(1, 13));

    private static NusModsSession S(WeekDay day, int start, int end) =>
        new("X", "Lecture", day, start, end, AllWeeks);

    [Fact]
    public void Compute_NoSessions_FullDaysFree()
    {
        var result = FreeSlotCalculator.Compute([[], []]);

        result.Count(f => f.Day == WeekDay.Monday).ShouldBe(1);
        var monday = result.Single(f => f.Day == WeekDay.Monday);
        monday.Start.ShouldBe("0800");
        monday.End.ShouldBe("2000");
        result.ShouldNotContain(f => f.Day == WeekDay.Saturday);
    }

    [Fact]
    public void Compute_SessionsFromDifferentParticipants_BothBlock()
    {
        var result = FreeSlotCalculator.Compute(
            [[S(WeekDay.Monday, 540, 600)], [S(WeekDay.Monday, 720, 780)]]
        );

        var monday = result.Where(f => f.Day == WeekDay.Monday).ToList();
        monday.ShouldBe(
            [
                new FreeSlot(WeekDay.Monday, "0800", "0900"),
                new FreeSlot(WeekDay.Monday, "1000", "1200"),
                new FreeSlot(WeekDay.Monday, "1300", "2000"),
            ]
        );
    }

    [Fact]
    public void Compute_AdjacentSessions_MergeIntoOneBlockedRange()
    {
        var result = FreeSlotCalculator.Compute(
            [[S(WeekDay.Tuesday, 540, 600), S(WeekDay.Tuesday, 600, 660)]]
        );

        result
            .Where(f => f.Day == WeekDay.Tuesday)
            .ShouldBe(
                [
                    new FreeSlot(WeekDay.Tuesday, "0800", "0900"),
                    new FreeSlot(WeekDay.Tuesday, "1100", "2000"),
                ]
            );
    }
}
