using Backend.Models;
using Backend.Services.NusMods;
using Google.OrTools.Sat;

namespace Backend.Services.Optimiser;

public enum SolveStatus
{
    Optimal,
    Feasible,
    Infeasible,
}

public record InfeasibleReason(
    string Reason,
    Guid? UserId,
    WeekDay? Day,
    string? ModuleCode,
    string? LessonType
);

public record SharedClass(
    string ModuleCode,
    string LessonType,
    string ClassNo,
    IReadOnlyList<Guid> UserIds
);

public record Solution(
    int Rank,
    long Score,
    IReadOnlyDictionary<(Guid UserId, string ModuleCode, string LessonType), string> Assignment,
    IReadOnlyList<SharedClass> SharedClasses
);

public record SolveResult(
    SolveStatus Status,
    IReadOnlyList<Solution> Solutions,
    IReadOnlyList<InfeasibleReason> InfeasibleReasons
);

public class SolverModelBuilder
{
    private const long OverlapWeight = 10;

    private sealed record VarKey(Guid UserId, string ModuleCode, string LessonType, string ClassNo);

    private sealed record PairVar(
        BoolVar Both,
        Guid A,
        Guid B,
        string Module,
        string LessonType,
        string ClassNo
    );

    private sealed record AssumptionContext(
        Guid UserId,
        WeekDay Day,
        string ModuleCode,
        string LessonType
    );

    private sealed record BuiltModel(
        CpModel Model,
        Dictionary<VarKey, BoolVar> Vars,
        List<PairVar> PairVars,
        LinearExpr Objective,
        List<BoolVar> Assumptions,
        Dictionary<int, AssumptionContext> AssumptionContexts
    );

    public SolveResult Solve(
        IReadOnlyList<ParticipantInput> participants,
        int maxSolutions,
        double timeLimitSeconds,
        int workers = 4
    )
    {
        var built = BuildModel(participants);
        var deadline = DateTime.UtcNow.AddSeconds(timeLimitSeconds);

        double Remaining() => Math.Max(0.5, (deadline - DateTime.UtcNow).TotalSeconds);

        var freeVars = built
            .Vars.Where(kv =>
            {
                var p = participants.First(x => x.UserId == kv.Key.UserId);
                var lesson = p.Lessons.First(l =>
                    l.ModuleCode == kv.Key.ModuleCode && l.LessonType == kv.Key.LessonType
                );
                return p.Mutable && !lesson.Locked && lesson.Options.Count > 1;
            })
            .ToList();

        var solutions = new List<Solution>();
        var provenOptimal = true;

        while (solutions.Count < maxSolutions)
        {
            var solver = new CpSolver
            {
                StringParameters =
                    $"max_time_in_seconds:{Remaining()},num_search_workers:{workers},random_seed:42",
            };
            var status = solver.Solve(built.Model);

            if (status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
            {
                if (solutions.Count == 0)
                    return new SolveResult(
                        SolveStatus.Infeasible,
                        [],
                        ExplainInfeasibility(built, solver, deadline)
                    );

                break;
            }

            if (solutions.Count == 0)
            {
                provenOptimal = status == CpSolverStatus.Optimal;
                built.Model.Add(built.Objective >= ScoreFloor((long)solver.ObjectiveValue));
            }

            solutions.Add(ExtractSolution(solver, built, solutions.Count + 1));

            var chosenFree = freeVars.Where(kv => solver.Value(kv.Value) == 1).ToList();
            if (chosenFree.Count < 2 || DateTime.UtcNow >= deadline)
                break;

            built.Model.Add(
                LinearExpr.Sum(chosenFree.Select(kv => (LinearExpr)kv.Value))
                    <= chosenFree.Count - 2
            );
        }

        return new SolveResult(
            provenOptimal ? SolveStatus.Optimal : SolveStatus.Feasible,
            solutions,
            []
        );
    }

    // Alternatives must stay within 10% of the best score, which flips direction
    // once penalties push the best score below zero.
    private static long ScoreFloor(long bestScore) =>
        bestScore >= 0
            ? (long)Math.Ceiling(bestScore * 0.9)
            : (long)Math.Floor(bestScore * 1.1);

    private static Solution ExtractSolution(CpSolver solver, BuiltModel built, int rank)
    {
        var assignment = new Dictionary<(Guid, string, string), string>();
        foreach (var (key, v) in built.Vars)
        {
            if (solver.Value(v) == 1)
                assignment[(key.UserId, key.ModuleCode, key.LessonType)] = key.ClassNo;
        }

        var sharedClasses = built
            .PairVars.Where(pv => solver.Value(pv.Both) == 1)
            .GroupBy(pv => (pv.Module, pv.LessonType, pv.ClassNo))
            .Select(g => new SharedClass(
                g.Key.Module,
                g.Key.LessonType,
                g.Key.ClassNo,
                [.. g.SelectMany(pv => new[] { pv.A, pv.B }).Distinct()]
            ))
            .ToList();

        return new Solution(rank, (long)solver.ObjectiveValue, assignment, sharedClasses);
    }

    private static List<InfeasibleReason> ExplainInfeasibility(
        BuiltModel built,
        CpSolver solver,
        DateTime deadline
    )
    {
        if (built.Assumptions.Count == 0)
            return [];

        var culprits = solver
            .SufficientAssumptionsForInfeasibility()
            .Where(built.AssumptionContexts.ContainsKey)
            .ToList();

        if (culprits.Count == 0)
            culprits = [.. built.AssumptionContexts.Keys];

        if (culprits.Count > 1 && culprits.Count == built.Assumptions.Count)
            culprits = NarrowByDeletion(built, culprits, deadline);

        return
        [
            .. culprits
                .Select(index => built.AssumptionContexts[index])
                .Select(c => new InfeasibleReason(
                    "blockedDayConflict",
                    c.UserId,
                    c.Day,
                    c.ModuleCode,
                    c.LessonType
                )),
        ];
    }

    // A whole-set answer says nothing useful, so drop assumptions one at a time and
    // keep the ones whose removal alone restores feasibility.
    private static List<int> NarrowByDeletion(
        BuiltModel built,
        List<int> candidates,
        DateTime deadline
    )
    {
        var necessary = new List<int>();

        foreach (var candidate in candidates)
        {
            if (DateTime.UtcNow >= deadline)
                return candidates;

            SetAssumptions(built, built.Assumptions.Where(a => a.Index != candidate));

            var probe = new CpSolver
            {
                StringParameters = "max_time_in_seconds:1,num_search_workers:1,random_seed:42",
            };
            if (probe.Solve(built.Model) is CpSolverStatus.Optimal or CpSolverStatus.Feasible)
                necessary.Add(candidate);
        }

        SetAssumptions(built, built.Assumptions);

        return necessary.Count > 0 ? necessary : candidates;
    }

    private static void SetAssumptions(BuiltModel built, IEnumerable<BoolVar> assumptions)
    {
        built.Model.Model.Assumptions.Clear();
        foreach (var assumption in assumptions)
            built.Model.AddAssumption(assumption);
    }

    private static BuiltModel BuildModel(IReadOnlyList<ParticipantInput> participants)
    {
        var model = new CpModel();
        var vars = new Dictionary<VarKey, BoolVar>();
        var objectiveTerms = new List<LinearExpr>();
        var assumptions = new Dictionary<(Guid UserId, WeekDay Day), BoolVar>();
        var assumptionContexts = new Dictionary<int, AssumptionContext>();

        foreach (var p in participants)
        {
            foreach (var lesson in p.Lessons)
            {
                var lessonVars = new List<BoolVar>();
                foreach (var option in lesson.Options)
                {
                    var v = model.NewBoolVar(
                        $"x_{p.UserId:N}_{lesson.ModuleCode}_{lesson.LessonType}_{option.ClassNo}"
                    );
                    vars[
                        new VarKey(p.UserId, lesson.ModuleCode, lesson.LessonType, option.ClassNo)
                    ] = v;
                    lessonVars.Add(v);

                    var isCurrent = option.ClassNo == lesson.CurrentClassNo;
                    if ((!p.Mutable || lesson.Locked) && !isCurrent)
                        model.Add(v == 0);

                    var blockedDays = option
                        .Sessions.Select(s => s.Day)
                        .Where(p.Prefs.BlockedDays.Contains)
                        .Distinct();

                    foreach (var day in blockedDays)
                    {
                        if (!assumptions.TryGetValue((p.UserId, day), out var assume))
                        {
                            assume = model.NewBoolVar($"assume_blocked_{p.UserId:N}_{day}");
                            assumptions[(p.UserId, day)] = assume;
                            model.AddAssumption(assume);
                            assumptionContexts[assume.Index] = new AssumptionContext(
                                p.UserId,
                                day,
                                lesson.ModuleCode,
                                lesson.LessonType
                            );
                        }

                        model.Add(v == 0).OnlyEnforceIf(assume);
                    }
                }

                model.AddExactlyOne(lessonVars);
            }

            var lessonList = p.Lessons;
            for (var i = 0; i < lessonList.Count; i++)
                for (var j = i + 1; j < lessonList.Count; j++)
                    foreach (var oa in lessonList[i].Options)
                        foreach (var ob in lessonList[j].Options)
                        {
                            if (!SessionsConflict(oa.Sessions, ob.Sessions))
                                continue;

                            var va = vars[
                                new VarKey(
                                    p.UserId,
                                    lessonList[i].ModuleCode,
                                    lessonList[i].LessonType,
                                    oa.ClassNo
                                )
                            ];
                            var vb = vars[
                                new VarKey(
                                    p.UserId,
                                    lessonList[j].ModuleCode,
                                    lessonList[j].LessonType,
                                    ob.ClassNo
                                )
                            ];
                            model.AddBoolOr([va.Not(), vb.Not()]);
                        }
        }

        var pairVars = new List<PairVar>();
        for (var i = 0; i < participants.Count; i++)
            for (var j = i + 1; j < participants.Count; j++)
            {
                var pa = participants[i];
                var pb = participants[j];
                foreach (var la in pa.Lessons)
                {
                    var lb = pb.Lessons.FirstOrDefault(l =>
                        l.ModuleCode == la.ModuleCode && l.LessonType == la.LessonType
                    );
                    if (lb is null)
                        continue;

                    foreach (var option in la.Options)
                    {
                        if (!lb.Options.Any(o => o.ClassNo == option.ClassNo))
                            continue;

                        var va = vars[
                            new VarKey(pa.UserId, la.ModuleCode, la.LessonType, option.ClassNo)
                        ];
                        var vb = vars[
                            new VarKey(pb.UserId, lb.ModuleCode, lb.LessonType, option.ClassNo)
                        ];
                        var both = model.NewBoolVar(
                            $"shared_{pa.UserId:N}_{pb.UserId:N}_{la.ModuleCode}_{option.ClassNo}"
                        );
                        model.AddBoolAnd([va, vb]).OnlyEnforceIf(both);
                        model.AddBoolOr([va.Not(), vb.Not(), both]);

                        var weeks = option.Sessions.SelectMany(s => s.Weeks).Distinct().Count();
                        objectiveTerms.Add(LinearExpr.Term(both, OverlapWeight * weeks));
                        pairVars.Add(
                            new PairVar(
                                both,
                                pa.UserId,
                                pb.UserId,
                                la.ModuleCode,
                                la.LessonType,
                                option.ClassNo
                            )
                        );
                    }
                }
            }

        var days = Enum.GetValues<WeekDay>();
        foreach (var p in participants)
        {
            var prefs = p.Prefs;

            foreach (var lesson in p.Lessons)
                foreach (var option in lesson.Options)
                {
                    var v = vars[
                        new VarKey(p.UserId, lesson.ModuleCode, lesson.LessonType, option.ClassNo)
                    ];
                    long penalty = 0;

                    foreach (var s in option.Sessions)
                    {
                        if (prefs.PreferredWindow is { } w)
                        {
                            var outside =
                                Math.Max(0, w.Start - s.StartMin) + Math.Max(0, s.EndMin - w.End);
                            penalty += PreferenceMerger.TierWeight(Tier.NiceToHave) * outside / 30;
                        }
                        if (prefs.EarliestStartMin is { } e && s.StartMin < e)
                            penalty += (e - s.StartMin) / 30;
                        if (prefs.LatestEndMin is { } l && s.EndMin > l)
                            penalty += (s.EndMin - l) / 30;
                    }

                    if (penalty > 0)
                        objectiveTerms.Add(LinearExpr.Term(v, -penalty));
                }

            var needDayStructures =
                prefs.FewerCampusDays != Tier.Off
                || prefs.CompactDays != Tier.Off
                || prefs.LunchBreak != Tier.Off
                || prefs.FreeDay is not null
                || prefs.MaxConsecutiveHours is not null;
            if (!needDayStructures)
                continue;

            foreach (var day in days)
            {
                var onDay = p
                    .Lessons.SelectMany(l => l.Options.Select(o => (Lesson: l, Option: o)))
                    .Where(x => x.Option.Sessions.Any(s => s.Day == day))
                    .ToList();
                if (onDay.Count == 0)
                    continue;

                var dayUsed = model.NewBoolVar($"dayused_{p.UserId:N}_{day}");
                var dayVars = new List<BoolVar>();
                foreach (var (lesson, option) in onDay)
                {
                    var v = vars[
                        new VarKey(p.UserId, lesson.ModuleCode, lesson.LessonType, option.ClassNo)
                    ];
                    model.AddImplication(v, dayUsed);
                    dayVars.Add(v);
                }
                model.AddBoolOr(dayVars.Select(v => (ILiteral)v).Append(dayUsed.Not()));

                if (prefs.FewerCampusDays != Tier.Off)
                {
                    objectiveTerms.Add(
                        LinearExpr.Term(
                            dayUsed,
                            -PreferenceMerger.TierWeight(prefs.FewerCampusDays) * 4
                        )
                    );
                }

                if (prefs.FreeDay == day)
                    objectiveTerms.Add(LinearExpr.Term(dayUsed, -6));

                if (prefs.CompactDays != Tier.Off)
                {
                    var first = model.NewIntVar(480, 1260, $"first_{p.UserId:N}_{day}");
                    var last = model.NewIntVar(480, 1260, $"last_{p.UserId:N}_{day}");
                    foreach (var (lesson, option) in onDay)
                    {
                        var v = vars[
                            new VarKey(
                                p.UserId,
                                lesson.ModuleCode,
                                lesson.LessonType,
                                option.ClassNo
                            )
                        ];
                        foreach (var s in option.Sessions.Where(s => s.Day == day))
                        {
                            model.Add(first <= s.StartMin).OnlyEnforceIf(v);
                            model.Add(last >= s.EndMin).OnlyEnforceIf(v);
                        }
                    }
                    model.Add(first == 480).OnlyEnforceIf(dayUsed.Not());
                    model.Add(last == 480).OnlyEnforceIf(dayUsed.Not());

                    var span = model.NewIntVar(0, 780, $"span_{p.UserId:N}_{day}");
                    model.Add(span == last - first);
                    var spanScaled = model.NewIntVar(0, 26, $"spanscaled_{p.UserId:N}_{day}");
                    model.AddDivisionEquality(spanScaled, span, model.NewConstant(30));
                    objectiveTerms.Add(
                        LinearExpr.Term(spanScaled, -PreferenceMerger.TierWeight(prefs.CompactDays))
                    );
                }

                if (prefs.LunchBreak != Tier.Off)
                {
                    var slotFrees = new List<BoolVar>();
                    foreach (
                        var (slotStart, slotEnd, k) in new[] { (660, 720, 0), (720, 780, 1), (780, 840, 2) }
                    )
                    {
                        var slotFree = model.NewBoolVar($"lunch_{p.UserId:N}_{day}_{k}");
                        foreach (var (lesson, option) in onDay)
                        {
                            var v = vars[
                                new VarKey(
                                    p.UserId,
                                    lesson.ModuleCode,
                                    lesson.LessonType,
                                    option.ClassNo
                                )
                            ];
                            if (
                                option.Sessions.Any(s =>
                                    s.Day == day && s.StartMin < slotEnd && slotStart < s.EndMin
                                )
                            )
                                model.AddImplication(v, slotFree.Not());
                        }
                        slotFrees.Add(slotFree);
                    }

                    var lunchOk = model.NewBoolVar($"lunchok_{p.UserId:N}_{day}");
                    model.AddBoolOr(slotFrees.Select(s => (ILiteral)s).Append(lunchOk.Not()));
                    var bonus = model.NewBoolVar($"lunchbonus_{p.UserId:N}_{day}");
                    model.AddBoolAnd([dayUsed, lunchOk]).OnlyEnforceIf(bonus);
                    objectiveTerms.Add(
                        LinearExpr.Term(bonus, PreferenceMerger.TierWeight(prefs.LunchBreak) * 2)
                    );
                }

                if (prefs.MaxConsecutiveHours is { } maxHours)
                {
                    var occ = new BoolVar[12];
                    for (var h = 0; h < 12; h++)
                    {
                        occ[h] = model.NewBoolVar($"occ_{p.UserId:N}_{day}_{h}");
                        var hourStart = 480 + h * 60;
                        var hourEnd = hourStart + 60;
                        foreach (var (lesson, option) in onDay)
                        {
                            var v = vars[
                                new VarKey(
                                    p.UserId,
                                    lesson.ModuleCode,
                                    lesson.LessonType,
                                    option.ClassNo
                                )
                            ];
                            if (
                                option.Sessions.Any(s =>
                                    s.Day == day && s.StartMin < hourEnd && hourStart < s.EndMin
                                )
                            )
                                model.AddImplication(v, occ[h]);
                        }
                    }
                    for (var start = 0; start + maxHours < 12; start++)
                    {
                        var windowVars = occ.Skip(start).Take(maxHours + 1).ToArray();
                        var over = model.NewBoolVar($"over_{p.UserId:N}_{day}_{start}");
                        model.Add(LinearExpr.Sum(windowVars) <= maxHours).OnlyEnforceIf(over.Not());
                        objectiveTerms.Add(LinearExpr.Term(over, -2));
                    }
                }
            }
        }

        var objective = LinearExpr.Sum(
            objectiveTerms.Count > 0 ? objectiveTerms : [model.NewConstant(0)]
        );
        model.Maximize(objective);

        return new BuiltModel(
            model,
            vars,
            pairVars,
            objective,
            [.. assumptions.Values],
            assumptionContexts
        );
    }

    internal static bool SessionsConflict(
        IReadOnlyList<NusModsSession> a,
        IReadOnlyList<NusModsSession> b
    ) =>
        a.Any(sa =>
            b.Any(sb =>
                sa.Day == sb.Day
                && sa.StartMin < sb.EndMin
                && sb.StartMin < sa.EndMin
                && sa.Weeks.Overlaps(sb.Weeks)
            )
        );
}
