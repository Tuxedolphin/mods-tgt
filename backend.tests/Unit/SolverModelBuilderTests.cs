using Backend.Models;
using Backend.Services.NusMods;
using Backend.Services.Optimiser;
using Shouldly;

namespace Backend.Tests.Unit;

public class SolverModelBuilderTests
{
    private static readonly IReadOnlySet<int> AllWeeks = new HashSet<int>(Enumerable.Range(1, 13));
    private static readonly IReadOnlySet<int> OddWeeks = new HashSet<int> { 1, 3, 5, 7, 9, 11, 13 };
    private static readonly IReadOnlySet<int> EvenWeeks = new HashSet<int> { 2, 4, 6, 8, 10, 12 };

    private static EffectivePreferences NoPrefs(params WeekDay[] blocked) =>
        new(
            BlockedDays: blocked.ToHashSet(),
            EarliestStartMin: null,
            LatestEndMin: null,
            PreferredWindow: null,
            LunchBreak: Tier.Off,
            CompactDays: Tier.Off,
            FewerCampusDays: Tier.Off,
            MaxConsecutiveHours: null,
            FreeDay: null,
            LockedLessons: [],
            UsedDefaults: false
        );

    private static NusModsSession S(
        string classNo,
        WeekDay day,
        int start,
        int end,
        IReadOnlySet<int>? weeks = null,
        string type = "Tutorial"
    ) => new(classNo, type, day, start, end, weeks ?? AllWeeks);

    private static LessonInput Lesson(
        string module,
        string current,
        bool locked,
        params LessonOption[] options
    ) => new(module, "Tutorial", current, locked, options);

    private static ParticipantInput P(
        Guid id,
        EffectivePreferences prefs,
        bool mutable,
        params LessonInput[] lessons
    ) => new(id, Guid.NewGuid(), mutable, prefs, lessons);

    private static SolverModelBuilder Builder() => new();

    private static EffectivePreferences Prefs(
        Tier lunch = Tier.Off,
        Tier compact = Tier.Off,
        Tier fewerDays = Tier.Off,
        int? earliest = null,
        (int, int)? window = null,
        WeekDay? freeDay = null,
        int? maxConsecutive = null
    ) =>
        new(
            BlockedDays: new HashSet<WeekDay>(),
            EarliestStartMin: earliest,
            LatestEndMin: null,
            PreferredWindow: window,
            LunchBreak: lunch,
            CompactDays: compact,
            FewerCampusDays: fewerDays,
            MaxConsecutiveHours: maxConsecutive,
            FreeDay: freeDay,
            LockedLessons: [],
            UsedDefaults: false
        );

    [Fact]
    public void Solve_TwoUsersOneSharedModule_PicksSameSlot()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var options = new[]
        {
            new LessonOption("T01", [S("T01", WeekDay.Monday, 600, 660)]),
            new LessonOption("T02", [S("T02", WeekDay.Tuesday, 600, 660)]),
        };

        var result = Builder()
            .Solve(
                [
                    P(a, NoPrefs(), true, Lesson("CS2100", "T01", false, options)),
                    P(b, NoPrefs(), true, Lesson("CS2100", "T02", false, options)),
                ],
                maxSolutions: 1,
                timeLimitSeconds: 5,
                workers: 1
            );

        result.Status.ShouldBe(SolveStatus.Optimal);
        var sol = result.Solutions.ShouldHaveSingleItem();
        sol.Assignment[(a, "CS2100", "Tutorial")]
            .ShouldBe(sol.Assignment[(b, "CS2100", "Tutorial")]);
        var shared = sol.SharedClasses.ShouldHaveSingleItem();
        shared.UserIds.ShouldBe([a, b], ignoreOrder: true);
    }

    [Fact]
    public void Solve_BlockedDay_NeverAssignsSlotOnThatDay()
    {
        var a = Guid.NewGuid();
        var options = new[]
        {
            new LessonOption("T01", [S("T01", WeekDay.Monday, 600, 660)]),
            new LessonOption("T02", [S("T02", WeekDay.Friday, 600, 660)]),
        };

        var result = Builder()
            .Solve(
                [P(a, NoPrefs(WeekDay.Monday), true, Lesson("CS2100", "T01", false, options))],
                1,
                5,
                1
            );

        result.Solutions.Single().Assignment[(a, "CS2100", "Tutorial")].ShouldBe("T02");
    }

    [Fact]
    public void Solve_LockedLesson_NeverMoves()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var options = new[]
        {
            new LessonOption("T01", [S("T01", WeekDay.Monday, 600, 660)]),
            new LessonOption("T02", [S("T02", WeekDay.Tuesday, 600, 660)]),
        };

        var result = Builder()
            .Solve(
                [
                    P(a, NoPrefs(), true, Lesson("CS2100", "T01", false, options)),
                    P(b, NoPrefs(), true, Lesson("CS2100", "T02", true, options)),
                ],
                1,
                5,
                1
            );

        var sol = result.Solutions.Single();
        sol.Assignment[(b, "CS2100", "Tutorial")].ShouldBe("T02");
        sol.Assignment[(a, "CS2100", "Tutorial")].ShouldBe("T02");
    }

    [Fact]
    public void Solve_ImmutableParticipant_KeepsAllCurrentSlots()
    {
        var a = Guid.NewGuid();
        var options = new[]
        {
            new LessonOption("T01", [S("T01", WeekDay.Monday, 600, 660)]),
            new LessonOption("T02", [S("T02", WeekDay.Tuesday, 600, 660)]),
        };

        var result = Builder()
            .Solve([P(a, NoPrefs(), false, Lesson("CS2100", "T01", false, options))], 1, 5, 1);

        result.Solutions.Single().Assignment[(a, "CS2100", "Tutorial")].ShouldBe("T01");
    }

    [Fact]
    public void Solve_ClashingOptions_AvoidsClash()
    {
        var a = Guid.NewGuid();
        var mod1 = Lesson(
            "CS2100",
            "T01",
            false,
            new LessonOption("T01", [S("T01", WeekDay.Monday, 600, 660)])
        );
        var mod2 = new LessonInput(
            "CS2101",
            "Tutorial",
            "V01",
            false,
            [
                new LessonOption("V01", [S("V01", WeekDay.Monday, 630, 690)]),
                new LessonOption("V02", [S("V02", WeekDay.Monday, 700, 760)]),
            ]
        );

        var result = Builder().Solve([P(a, NoPrefs(), true, mod1, mod2)], 1, 5, 1);

        result.Solutions.Single().Assignment[(a, "CS2101", "Tutorial")].ShouldBe("V02");
    }

    [Fact]
    public void Solve_OddEvenWeekLessons_DoNotFalseClash()
    {
        var a = Guid.NewGuid();
        var mod1 = Lesson(
            "CS2100",
            "T01",
            false,
            new LessonOption("T01", [S("T01", WeekDay.Monday, 600, 660, OddWeeks)])
        );
        var mod2 = new LessonInput(
            "CS2101",
            "Tutorial",
            "V01",
            false,
            [new LessonOption("V01", [S("V01", WeekDay.Monday, 600, 660, EvenWeeks)])]
        );

        var result = Builder().Solve([P(a, NoPrefs(), true, mod1, mod2)], 1, 5, 1);

        result.Status.ShouldBe(SolveStatus.Optimal);
    }

    [Fact]
    public void Solve_BlockedDayMakesLessonImpossible_ReturnsInfeasible()
    {
        var a = Guid.NewGuid();
        var result = Builder()
            .Solve(
                [
                    P(
                        a,
                        NoPrefs(WeekDay.Monday),
                        true,
                        Lesson(
                            "CS2100",
                            "T01",
                            false,
                            new LessonOption("T01", [S("T01", WeekDay.Monday, 600, 660)])
                        )
                    ),
                ],
                1,
                5,
                1
            );

        result.Status.ShouldBe(SolveStatus.Infeasible);
        result.Solutions.ShouldBeEmpty();
    }

    [Fact]
    public void Solve_SameInputTwice_SameOutput()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var options = Enumerable
            .Range(1, 5)
            .Select(i => new LessonOption(
                $"T0{i}",
                [S($"T0{i}", WeekDay.Monday, 540 + i * 60, 600 + i * 60)]
            ))
            .ToArray();

        ParticipantInput[] Input() =>
            [
                P(a, NoPrefs(), true, Lesson("CS2100", "T01", false, options)),
                P(b, NoPrefs(), true, Lesson("CS2100", "T03", false, options)),
            ];

        var first = Builder().Solve(Input(), 1, 5, 1);
        var second = Builder().Solve(Input(), 1, 5, 1);

        first.Solutions.Single().Assignment.ShouldBe(second.Solutions.Single().Assignment);
    }

    [Fact]
    public void Solve_PreferredWindow_PicksSlotInsideWindow()
    {
        var a = Guid.NewGuid();
        var options = new[]
        {
            new LessonOption("T01", [S("T01", WeekDay.Monday, 480, 540)]),
            new LessonOption("T02", [S("T02", WeekDay.Monday, 840, 900)]),
        };

        var result = Builder()
            .Solve(
                [P(a, Prefs(window: (720, 1080)), true, Lesson("CS2100", "T01", false, options))],
                1,
                5,
                1
            );

        result.Solutions.Single().Assignment[(a, "CS2100", "Tutorial")].ShouldBe("T02");
    }

    [Fact]
    public void Solve_EarliestStart_AvoidsEarlySlot()
    {
        var a = Guid.NewGuid();
        var options = new[]
        {
            new LessonOption("T01", [S("T01", WeekDay.Monday, 480, 540)]),
            new LessonOption("T02", [S("T02", WeekDay.Monday, 600, 660)]),
        };

        var result = Builder()
            .Solve(
                [P(a, Prefs(earliest: 540), true, Lesson("CS2100", "T01", false, options))],
                1,
                5,
                1
            );

        result.Solutions.Single().Assignment[(a, "CS2100", "Tutorial")].ShouldBe("T02");
    }

    [Fact]
    public void Solve_FewerCampusDays_ConsolidatesOntoOneDay()
    {
        var a = Guid.NewGuid();
        var mod1 = Lesson(
            "CS2100",
            "T01",
            false,
            new LessonOption("T01", [S("T01", WeekDay.Monday, 600, 660)])
        );
        var mod2 = new LessonInput(
            "CS2101",
            "Tutorial",
            "V02",
            false,
            [
                new LessonOption("V01", [S("V01", WeekDay.Monday, 700, 760)]),
                new LessonOption("V02", [S("V02", WeekDay.Tuesday, 700, 760)]),
            ]
        );

        var result = Builder()
            .Solve([P(a, Prefs(fewerDays: Tier.Important), true, mod1, mod2)], 1, 5, 1);

        result.Solutions.Single().Assignment[(a, "CS2101", "Tutorial")].ShouldBe("V01");
    }

    [Fact]
    public void Solve_LunchBreak_KeepsAnHourFree()
    {
        var a = Guid.NewGuid();
        var mod1 = Lesson(
            "CS2100",
            "T01",
            false,
            new LessonOption("T01", [S("T01", WeekDay.Monday, 660, 720)])
        );
        var mod2 = new LessonInput(
            "CS2101",
            "Tutorial",
            "V01",
            false,
            [
                new LessonOption("V01", [S("V01", WeekDay.Monday, 720, 780)]),
                new LessonOption("V02", [S("V02", WeekDay.Monday, 900, 960)]),
            ]
        );

        var result = Builder()
            .Solve([P(a, Prefs(lunch: Tier.Important), true, mod1, mod2)], 1, 5, 1);

        result.Solutions.Single().Assignment[(a, "CS2101", "Tutorial")].ShouldBe("V02");
    }

    [Fact]
    public void Solve_FreeDayWish_ClearsThatDay()
    {
        var a = Guid.NewGuid();
        var options = new[]
        {
            new LessonOption("T01", [S("T01", WeekDay.Friday, 600, 660)]),
            new LessonOption("T02", [S("T02", WeekDay.Monday, 600, 660)]),
        };

        var result = Builder()
            .Solve(
                [
                    P(
                        a,
                        Prefs(freeDay: WeekDay.Friday),
                        true,
                        Lesson("CS2100", "T01", false, options)
                    ),
                ],
                1,
                5,
                1
            );

        result.Solutions.Single().Assignment[(a, "CS2100", "Tutorial")].ShouldBe("T02");
    }

    [Fact]
    public void Solve_OverlapOutweighsMildPreference_WhenConflicting()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var options = new[]
        {
            new LessonOption("T01", [S("T01", WeekDay.Monday, 480, 540)]),
            new LessonOption("T02", [S("T02", WeekDay.Monday, 840, 900)]),
        };

        var result = Builder()
            .Solve(
                [
                    P(a, Prefs(window: (720, 1080)), true, Lesson("CS2100", "T01", false, options)),
                    P(
                        b,
                        NoPrefs(),
                        true,
                        new LessonInput("CS2100", "Tutorial", "T01", true, [options[0]])
                    ),
                ],
                1,
                5,
                1
            );

        result.Solutions.Single().Assignment[(a, "CS2100", "Tutorial")].ShouldBe("T01");
    }

    [Fact]
    public void Solve_MaxSolutions3_ReturnsDistinctRankedSolutions()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var options = Enumerable
            .Range(1, 6)
            .Select(i => new LessonOption(
                $"T{i:00}",
                [S($"T{i:00}", WeekDay.Monday, 480 + i * 60, 540 + i * 60)]
            ))
            .ToArray();

        var result = Builder()
            .Solve(
                [
                    P(a, NoPrefs(), true, Lesson("CS2100", "T01", false, options)),
                    P(b, NoPrefs(), true, Lesson("CS2100", "T02", false, options)),
                ],
                maxSolutions: 3,
                timeLimitSeconds: 10,
                workers: 1
            );

        result.Solutions.Count.ShouldBeGreaterThan(1);
        result.Solutions.Select(s => s.Rank).ShouldBe(Enumerable.Range(1, result.Solutions.Count));
        result.Solutions.ShouldAllBe(s => s.SharedClasses.Count == 1);
        result
            .Solutions.Select(s => s.Assignment[(a, "CS2100", "Tutorial")])
            .Distinct()
            .Count()
            .ShouldBe(result.Solutions.Count);
        result
            .Solutions.Zip(result.Solutions.Skip(1))
            .ShouldAllBe(z => z.First.Score >= z.Second.Score);
    }

    [Fact]
    public void Solve_SingleOptionEverywhere_ReturnsOneSolutionOnly()
    {
        var a = Guid.NewGuid();
        var result = Builder()
            .Solve(
                [
                    P(
                        a,
                        NoPrefs(),
                        true,
                        Lesson(
                            "CS2100",
                            "T01",
                            false,
                            new LessonOption("T01", [S("T01", WeekDay.Monday, 600, 660)])
                        )
                    ),
                ],
                maxSolutions: 3,
                timeLimitSeconds: 5,
                workers: 1
            );

        result.Solutions.Count.ShouldBe(1);
    }

    [Fact]
    public void Solve_InfeasibleBlockedDay_ReturnsReasonNamingUserAndDay()
    {
        var a = Guid.NewGuid();
        var result = Builder()
            .Solve(
                [
                    P(
                        a,
                        NoPrefs(WeekDay.Monday),
                        true,
                        Lesson(
                            "CS2100",
                            "T01",
                            false,
                            new LessonOption("T01", [S("T01", WeekDay.Monday, 600, 660)])
                        )
                    ),
                ],
                1,
                5,
                1
            );

        result.Status.ShouldBe(SolveStatus.Infeasible);
        var reason = result.InfeasibleReasons.ShouldHaveSingleItem();
        reason.Reason.ShouldBe("blockedDayConflict");
        reason.UserId.ShouldBe(a);
        reason.Day.ShouldBe(WeekDay.Monday);
        reason.ModuleCode.ShouldBe("CS2100");
    }
}
