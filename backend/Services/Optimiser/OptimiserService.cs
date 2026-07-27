using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Data;
using Backend.DTOs;
using Backend.Exceptions;
using Backend.Hubs;
using Backend.Hubs.Clients;
using Backend.Models;
using Backend.Services.NusMods;
using Backend.Services.Rooms;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Optimiser;

public class OptimiserService(
    AppDbContext context,
    IRoomMembershipChecker membershipChecker,
    LessonCatalogueBuilder catalogueBuilder,
    SolverModelBuilder solver,
    INusModsClient nusModsClient,
    IRoomTracker tracker,
    IHubContext<RoomHub, IRoomHubClient> hub,
    IRoomService roomService
) : IOptimiserService
{
    private const int MaxSolutions = 3;
    private const double TimeBudgetSeconds = 10.0;
    private const int MaxModuleFetchConcurrency = 6;

    private static readonly TimeZoneInfo CampusTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        "Asia/Singapore"
    );

    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public Task<SolveResponse> SolveGroupAsync(Guid roomId, Guid userId, SolveRequest request) =>
        SolveAsync(
            roomId,
            userId,
            request.Participants,
            request.LockedLessons,
            null,
            request.IncludeFreeSlots,
            soloCaller: null
        );

    public Task<SolveResponse> SolveSoloAsync(Guid roomId, Guid userId, SoloSolveRequest request) =>
        SolveAsync(
            roomId,
            userId,
            null,
            request.LockedLessons,
            request.Preferences,
            request.IncludeFreeSlots,
            soloCaller: userId
        );

    private async Task<SolveResponse> SolveAsync(
        Guid roomId,
        Guid callerId,
        List<SolveParticipantRequest>? requestedParticipants,
        List<LockedLesson>? inlineLocks,
        PreferencePayload? inlinePrefs,
        bool includeFreeSlots,
        Guid? soloCaller
    )
    {
        await membershipChecker.EnsureMemberAsync(roomId, callerId);

        var timetables = await LoadTimetablesAsync(roomId);

        var anchor =
            timetables.FirstOrDefault(t => t.Id == roomId)
            ?? throw new NotFoundException("Room has no main timetable");

        var selected = SelectParticipantTimetables(timetables, requestedParticipants, anchor);

        var warnings = new List<SolveWarning>();
        var participants = new List<ParticipantInput>();
        var usedDefaults = new List<Guid>();

        var preferences = await LoadPreferencesAsync(
            roomId,
            [.. selected.Select(s => s.UserId)]
        );

        await PrefetchModulesAsync(selected, anchor);

        foreach (var (participantUserId, timetable) in selected)
        {
            preferences.TryGetValue((participantUserId, null), out var globalPrefs);
            preferences.TryGetValue((participantUserId, roomId), out var roomPrefs);
            var effective =
                participantUserId == callerId && inlinePrefs is not null
                    ? PreferenceMerger.Merge(globalPrefs, inlinePrefs)
                    : PreferenceMerger.Merge(globalPrefs, roomPrefs);

            if (effective.UsedDefaults)
                usedDefaults.Add(participantUserId);

            var locks = effective
                .LockedLessons.Select(l => (l.ModuleCode, l.LessonType))
                .ToHashSet();

            if (participantUserId == callerId && inlineLocks is not null)
                foreach (var l in inlineLocks)
                    locks.Add((l.ModuleCode, l.LessonType));

            var (lessons, lessonWarnings) = await catalogueBuilder.BuildAsync(
                participantUserId,
                timetable.MetaData,
                anchor.AcademicYear,
                anchor.Semester,
                locks
            );
            warnings.AddRange(lessonWarnings);

            if (participantUserId == callerId && inlineLocks is not null)
                warnings.AddRange(
                    inlineLocks
                        .Where(l =>
                            !lessons.Any(x =>
                                x.ModuleCode == l.ModuleCode && x.LessonType == l.LessonType
                            )
                        )
                        .Select(l => new SolveWarning(
                            "invalidLock",
                            callerId,
                            l.ModuleCode,
                            l.LessonType,
                            null
                        ))
                );

            participants.Add(
                new ParticipantInput(
                    participantUserId,
                    timetable.Id,
                    Mutable: soloCaller is null || participantUserId == soloCaller,
                    effective,
                    lessons
                )
            );
        }

        var result = solver.Solve(participants, MaxSolutions, TimeBudgetSeconds);

        var full = BuildFullResponse(
            result,
            participants,
            warnings,
            usedDefaults,
            soloCaller,
            includeFreeSlots
        );

        await PersistResultAsync(roomId, soloCaller, callerId, full);

        return full;
    }

    public async Task<SolveResponse> GetStoredResultAsync(Guid roomId, Guid userId)
    {
        await membershipChecker.EnsureMemberAsync(roomId, userId);

        var row =
            await context
                .OptimiserResults.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoomId == roomId && r.UserId == null)
            ?? throw new NotFoundException("No optimiser result for this room");

        return FilterForReader(Deserialize(row.PayloadJson), userId);
    }

    private static SolveResponse Deserialize(string payloadJson) =>
        JsonSerializer.Deserialize<SolveResponse>(payloadJson, PayloadOptions)
        ?? throw new NotFoundException("Stored optimiser result could not be read");

    public async Task<Guid> SaveSuggestionAsync(
        Guid roomId,
        Guid userId,
        SaveSuggestionRequest request
    )
    {
        await membershipChecker.EnsureMemberAsync(roomId, userId);

        var rows = await context
            .OptimiserResults.AsNoTracking()
            .Where(r => r.RoomId == roomId && (r.UserId == null || r.UserId == userId))
            .ToListAsync();

        if (rows.Count == 0)
            throw new NotFoundException("No optimiser result for this room");

        var row =
            rows.FirstOrDefault(r => r.SolveId == request.SolveId)
            ?? throw new ConflictException("Result superseded");

        var stored = Deserialize(row.PayloadJson);

        if (request.Rank < 1 || request.Rank > stored.Solutions.Count)
            throw new BadRequestException("Rank is out of range for this result");

        var solution = stored.Solutions[request.Rank - 1];
        var suggestion = solution.Suggestions.FirstOrDefault(s => s.UserId == userId);

        var timetables = await LoadTimetablesAsync(roomId);
        var source =
            (
                suggestion is null
                    ? LatestFor(timetables, userId)
                    : timetables.FirstOrDefault(t => t.Id == suggestion.TimetableId)
            ) ?? throw new NotFoundException("No timetable in this room for this user");

        var metaData = ApplyChanges(source.MetaData, suggestion?.Changes ?? []);

        var newTimetable = new RoomTimetable
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = $"Optimised {TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CampusTimeZone):dd MMM}",
            Semester = source.Semester,
            AcademicYear = source.AcademicYear,
            MetaData = metaData,
            RoomId = roomId,
            OriginalTimetableId = null,
        };

        if (tracker.RoomExists(roomId))
        {
            tracker.AddOrUpdateTimetable(newTimetable);

            var detailed = await roomService.GetTimetablesDetailedInRoomAsync(roomId, userId);
            if (detailed is not null)
                await hub.Clients.Group(roomId.ToString()).ReceiveTimetableUpdate(detailed);
        }
        else
        {
            context.Timetables.Add(
                new Timetable
                {
                    Id = newTimetable.Id,
                    UserId = userId,
                    Name = newTimetable.Name,
                    Semester = newTimetable.Semester,
                    AcademicYear = newTimetable.AcademicYear,
                    MetaData = metaData,
                    RoomId = roomId,
                }
            );
            await context.SaveChangesAsync();
        }

        return newTimetable.Id;
    }

    private static List<TimetableModule> ApplyChanges(
        ICollection<TimetableModule> source,
        List<LessonChangeResponse> changes
    )
    {
        var fallbackColour = source.FirstOrDefault()?.Colour ?? "#e0e0e0";

        var result = source
            .Select(m => new TimetableModule
            {
                ModuleCode = m.ModuleCode,
                LessonNo = m.LessonNo,
                LessonType = m.LessonType,
                Colour = m.Colour,
            })
            .ToList();

        foreach (var change in changes)
        {
            var existing = result.FirstOrDefault(m =>
                m.ModuleCode == change.ModuleCode && m.LessonType == change.LessonType
            );

            if (existing is null)
                result.Add(
                    new TimetableModule
                    {
                        ModuleCode = change.ModuleCode,
                        LessonNo = change.To,
                        LessonType = change.LessonType,
                        Colour =
                            source.FirstOrDefault(m => m.ModuleCode == change.ModuleCode)?.Colour
                            ?? fallbackColour,
                    }
                );
            else
                existing.LessonNo = change.To;
        }

        return result;
    }

    private async Task PersistResultAsync(
        Guid roomId,
        Guid? soloUserId,
        Guid callerId,
        SolveResponse response
    )
    {
        var payload = JsonSerializer.Serialize(response, PayloadOptions);
        var row = await context.OptimiserResults.FirstOrDefaultAsync(r =>
            r.RoomId == roomId && r.UserId == soloUserId
        );

        if (row is null)
        {
            context.OptimiserResults.Add(
                new OptimiserResult
                {
                    RoomId = roomId,
                    UserId = soloUserId,
                    RequestedBy = callerId,
                    SolveId = response.SolveId,
                    PayloadJson = payload,
                }
            );
        }
        else
        {
            row.RequestedBy = callerId;
            row.SolveId = response.SolveId;
            row.PayloadJson = payload;
            row.CreatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    private static SolveResponse FilterForReader(SolveResponse response, Guid readerId) =>
        OptimiserResultFilter.ForReader(response, readerId);

    public async Task<IReadOnlyList<Guid>> GetRoomAudienceAsync(Guid roomId)
    {
        var members = await context
            .RoomMembers.AsNoTracking()
            .Where(m => m.RoomId == roomId)
            .Select(m => m.UserId)
            .ToListAsync();

        var owners = (await LoadTimetablesAsync(roomId))
            .Where(t => t.UserId is not null)
            .Select(t => t.UserId!.Value);

        return [.. members.Concat(owners).Distinct()];
    }

    private async Task<List<RoomTimetable>> LoadTimetablesAsync(Guid roomId)
    {
        if (tracker.TryGetTimetablesInRoom(roomId, out var tracked))
            return [.. tracked];

        return await context
            .Timetables.AsNoTracking()
            .Where(t => t.RoomId == roomId)
            .Select(t => new RoomTimetable
            {
                Id = t.Id,
                UserId = t.UserId,
                Name = t.Name,
                Semester = t.Semester,
                AcademicYear = t.AcademicYear,
                MetaData = t.MetaData,
                RoomId = t.RoomId,
                OriginalTimetableId = t.OriginalTimetableId,
                CreatedAt = t.CreatedAt,
            })
            .ToListAsync();
    }

    // Warms the module cache so the per-participant catalogue build does no network I/O.
    // Members of a room overlap heavily, so the distinct set is far smaller than the total.
    private async Task PrefetchModulesAsync(
        List<(Guid UserId, RoomTimetable Timetable)> selected,
        RoomTimetable anchor
    )
    {
        var moduleCodes = selected
            .SelectMany(s => s.Timetable.MetaData.Select(m => m.ModuleCode))
            .Distinct()
            .ToList();

        using var throttle = new SemaphoreSlim(MaxModuleFetchConcurrency);

        await Task.WhenAll(
            moduleCodes.Select(async code =>
            {
                await throttle.WaitAsync();
                try
                {
                    await nusModsClient.GetModuleAsync(
                        anchor.AcademicYear,
                        anchor.Semester,
                        code
                    );
                }
                finally
                {
                    throttle.Release();
                }
            })
        );
    }

    private async Task<
        Dictionary<(Guid UserId, Guid? RoomId), PreferencePayload>
    > LoadPreferencesAsync(Guid roomId, List<Guid> userIds) =>
        await context
            .OptimiserPreferences.AsNoTracking()
            .Where(p =>
                userIds.Contains(p.UserId) && (p.RoomId == null || p.RoomId == roomId)
            )
            .ToDictionaryAsync(p => (p.UserId, p.RoomId), p => p.Payload);

    private static List<(Guid UserId, RoomTimetable Timetable)> SelectParticipantTimetables(
        List<RoomTimetable> timetables,
        List<SolveParticipantRequest>? requested,
        RoomTimetable anchor
    )
    {
        var selected = new List<(Guid UserId, RoomTimetable Timetable)>();

        if (requested is { Count: > 0 })
        {
            foreach (var participant in requested)
            {
                var timetable =
                    participant.TimetableId is { } id
                        ? timetables.FirstOrDefault(t =>
                            t.Id == id && t.UserId == participant.UserId
                        )
                        : LatestFor(timetables, participant.UserId);

                if (timetable is null)
                    throw new BadRequestException(
                        $"No timetable in this room for user {participant.UserId}"
                    );

                selected.Add((participant.UserId, timetable));
            }
        }
        else
        {
            selected.AddRange(
                timetables
                    .Where(t => t.UserId is not null)
                    .GroupBy(t => t.UserId!.Value)
                    .Select(g => (g.Key, g.OrderByDescending(t => t.CreatedAt).First()))
            );
        }

        foreach (var (_, timetable) in selected)
        {
            if (
                timetable.Semester != anchor.Semester
                || timetable.AcademicYear != anchor.AcademicYear
            )
                throw new BadRequestException(
                    "Selected timetables must share the room's semester and academic year"
                );
        }

        return [.. selected.OrderBy(s => s.UserId)];
    }

    private static RoomTimetable? LatestFor(List<RoomTimetable> timetables, Guid userId) =>
        timetables
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault();

    private static SolveResponse BuildFullResponse(
        SolveResult result,
        List<ParticipantInput> participants,
        List<SolveWarning> warnings,
        List<Guid> usedDefaults,
        Guid? soloCaller,
        bool includeFreeSlots
    )
    {
        var solutions = result
            .Solutions.Select(solution => MapSolution(solution, participants, includeFreeSlots))
            .ToList();

        var frozen = soloCaller is null
            ? []
            : participants
                .Where(p => !p.Mutable)
                .Select(p => new FrozenParticipantResponse
                {
                    UserId = p.UserId,
                    LessonCount = p.Lessons.Count,
                })
                .ToList();

        return new SolveResponse
        {
            Status = result.Status.ToString().ToLowerInvariant(),
            SolveId = Guid.NewGuid(),
            Solutions = solutions,
            Warnings =
            [
                .. warnings.Select(w => new WarningResponse
                {
                    Code = w.Code,
                    UserId = w.UserId,
                    ModuleCode = w.ModuleCode,
                    LessonType = w.LessonType,
                    Detail = w.Detail,
                }),
            ],
            UsedDefaults = usedDefaults,
            Frozen = frozen,
            InfeasibleReasons =
                result.InfeasibleReasons.Count == 0
                    ? null
                    :
                    [
                        .. result.InfeasibleReasons.Select(r => new InfeasibleReasonResponse
                        {
                            Reason = r.Reason,
                            UserId = r.UserId,
                            Day = r.Day,
                            ModuleCode = r.ModuleCode,
                            LessonType = r.LessonType,
                        }),
                    ],
        };
    }

    private static SolutionResponse MapSolution(
        Solution solution,
        List<ParticipantInput> participants,
        bool includeFreeSlots
    )
    {
        var suggestions = new List<SuggestionResponse>();
        var perUser = new List<UserScoreResponse>();
        var assignedSessions = new List<IReadOnlyList<NusModsSession>>();

        foreach (var participant in participants)
        {
            var changes = new List<LessonChangeResponse>();
            var sessions = new List<NusModsSession>();

            foreach (var lesson in participant.Lessons)
            {
                if (
                    !solution.Assignment.TryGetValue(
                        (participant.UserId, lesson.ModuleCode, lesson.LessonType),
                        out var classNo
                    )
                )
                    continue;

                if (classNo != lesson.CurrentClassNo)
                    changes.Add(
                        new LessonChangeResponse
                        {
                            ModuleCode = lesson.ModuleCode,
                            LessonType = lesson.LessonType,
                            From = lesson.CurrentClassNo,
                            To = classNo,
                        }
                    );

                var option = lesson.Options.FirstOrDefault(o => o.ClassNo == classNo);
                if (option is not null)
                    sessions.AddRange(option.Sessions);
            }

            if (changes.Count > 0)
                suggestions.Add(
                    new SuggestionResponse
                    {
                        UserId = participant.UserId,
                        TimetableId = participant.TimetableId,
                        Changes = changes,
                    }
                );

            assignedSessions.Add(sessions);

            var (satisfied, violated) = PreferenceEvaluator.Evaluate(participant.Prefs, sessions);
            perUser.Add(
                new UserScoreResponse
                {
                    UserId = participant.UserId,
                    SatisfiedCount = satisfied.Count,
                    ViolatedCount = violated.Count,
                    Satisfied = satisfied,
                    Violated = violated,
                }
            );
        }

        var sharedClasses = solution
            .SharedClasses.Select(s => new SharedClassResponse
            {
                ModuleCode = s.ModuleCode,
                LessonType = s.LessonType,
                ClassNo = s.ClassNo,
                UserIds = [.. s.UserIds],
            })
            .ToList();

        return new SolutionResponse
        {
            Rank = solution.Rank,
            Score = new ScoreResponse
            {
                SharedClassCount = sharedClasses.Count,
                SharedClasses = sharedClasses,
                PerUser = perUser,
            },
            Suggestions = suggestions,
            FreeSlots = includeFreeSlots
                ? [.. FreeSlotCalculator.Compute(assignedSessions)]
                : null,
        };
    }
}

internal static class PreferenceEvaluator
{
    private static readonly (int Start, int End)[] LunchSlots =
    [
        (660, 720),
        (720, 780),
        (780, 840),
    ];

    public static (
        List<PreferenceOutcomeResponse> Satisfied,
        List<PreferenceOutcomeResponse> Violated
    ) Evaluate(EffectivePreferences prefs, IReadOnlyList<NusModsSession> sessions)
    {
        var satisfied = new List<PreferenceOutcomeResponse>();
        var violated = new List<PreferenceOutcomeResponse>();

        if (prefs.EarliestStartMin is { } earliest)
            Classify(
                "earliestStart",
                sessions.Where(s => s.StartMin < earliest),
                s => $"{NusModsParsing.ToHhmm(s.StartMin)} starts before {NusModsParsing.ToHhmm(earliest)}",
                satisfied,
                violated
            );

        if (prefs.LatestEndMin is { } latest)
            Classify(
                "latestEnd",
                sessions.Where(s => s.EndMin > latest),
                s => $"{NusModsParsing.ToHhmm(s.EndMin)} ends after {NusModsParsing.ToHhmm(latest)}",
                satisfied,
                violated
            );

        if (prefs.PreferredWindow is { } window)
            Classify(
                "preferredWindow",
                sessions.Where(s => s.StartMin < window.Start || s.EndMin > window.End),
                s =>
                    $"{NusModsParsing.ToHhmm(s.StartMin)}-{NusModsParsing.ToHhmm(s.EndMin)} falls outside "
                    + $"{NusModsParsing.ToHhmm(window.Start)}-{NusModsParsing.ToHhmm(window.End)}",
                satisfied,
                violated
            );

        if (prefs.FreeDay is { } freeDay)
            Classify(
                "freeDay",
                sessions.Where(s => s.Day == freeDay),
                s => $"{s.Day} still has a class at {NusModsParsing.ToHhmm(s.StartMin)}",
                satisfied,
                violated
            );

        if (prefs.LunchBreak != Tier.Off)
        {
            foreach (var day in sessions.Select(s => s.Day).Distinct().Order())
            {
                var onDay = sessions.Where(s => s.Day == day).ToList();
                var hasFreeSlot = LunchSlots.Any(slot =>
                    !onDay.Any(s => s.StartMin < slot.End && slot.Start < s.EndMin)
                );

                var outcome = new PreferenceOutcomeResponse { Pref = "lunchBreak", Day = day };
                if (hasFreeSlot)
                    satisfied.Add(outcome);
                else
                    violated.Add(outcome with { Detail = "No free hour between 1100 and 1400" });
            }
        }

        return (satisfied, violated);
    }

    private static void Classify(
        string pref,
        IEnumerable<NusModsSession> offenders,
        Func<NusModsSession, string> describe,
        List<PreferenceOutcomeResponse> satisfied,
        List<PreferenceOutcomeResponse> violated
    )
    {
        var offending = offenders.ToList();

        if (offending.Count == 0)
        {
            satisfied.Add(new PreferenceOutcomeResponse { Pref = pref });
            return;
        }

        violated.AddRange(
            offending.Select(s => new PreferenceOutcomeResponse
            {
                Pref = pref,
                Day = s.Day,
                Detail = describe(s),
            })
        );
    }
}
