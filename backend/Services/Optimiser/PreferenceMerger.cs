using Backend.Models;
using Backend.Services.NusMods;

namespace Backend.Services.Optimiser;

public record EffectivePreferences(
    IReadOnlySet<WeekDay> BlockedDays,
    int? EarliestStartMin,
    int? LatestEndMin,
    (int Start, int End)? PreferredWindow,
    Tier LunchBreak,
    Tier CompactDays,
    Tier FewerCampusDays,
    int? MaxConsecutiveHours,
    WeekDay? FreeDay,
    IReadOnlyList<LockedLesson> LockedLessons,
    bool UsedDefaults
);

public static class PreferenceMerger
{
    public static int TierWeight(Tier tier) =>
        tier switch
        {
            Tier.Off => 0,
            Tier.NiceToHave => 1,
            Tier.Important => 5,
            _ => 0,
        };

    public static EffectivePreferences Merge(PreferencePayload? global, PreferencePayload? room)
    {
        var usedDefaults = global is null && room is null;

        T? Pick<T>(Func<PreferencePayload, T?> get)
            where T : class =>
            (room is null ? null : get(room)) ?? (global is null ? null : get(global));

        T? PickValue<T>(Func<PreferencePayload, T?> get)
            where T : struct =>
            (room is null ? null : get(room)) ?? (global is null ? null : get(global));

        var earliest = Pick(p => p.EarliestStart);
        var latest = Pick(p => p.LatestEnd);
        var window = Pick(p => p.PreferredWindow);

        return new EffectivePreferences(
            BlockedDays: Pick(p => p.BlockedDays)?.ToHashSet() ?? [],
            EarliestStartMin: earliest is not null
                ? NusModsParsing.ParseTimeToMinutes(earliest)
                : (usedDefaults ? 540 : null),
            LatestEndMin: latest is not null ? NusModsParsing.ParseTimeToMinutes(latest) : null,
            PreferredWindow: window is not null
                ? (
                    NusModsParsing.ParseTimeToMinutes(window.Start),
                    NusModsParsing.ParseTimeToMinutes(window.End)
                )
                : null,
            LunchBreak: PickValue(p => p.LunchBreak) ?? (usedDefaults ? Tier.NiceToHave : Tier.Off),
            CompactDays: PickValue(p => p.CompactDays) ?? Tier.Off,
            FewerCampusDays: PickValue(p => p.FewerCampusDays) ?? Tier.Off,
            MaxConsecutiveHours: PickValue(p => p.MaxConsecutiveHours),
            FreeDay: PickValue(p => p.FreeDay),
            LockedLessons: room?.LockedLessons ?? [],
            UsedDefaults: usedDefaults
        );
    }
}
