using Backend.DTOs;
using Backend.Models;
using Backend.Services.Rooms;
using Shouldly;

namespace Backend.Tests.Services;

// The response DTOs are records holding List<TimetableModule>, so their generated equality
// compares the list by reference. These helpers compare field-by-field, with MetaData
// compared element-wise, so value-identical objects assert as equal.
public static class AssertExtensions
{
    public static void ShouldMatch(
        this TimetableSummaryResponse actual,
        TimetableSummaryResponse expected
    )
    {
        actual.Id.ShouldBe(expected.Id);
        actual.Name.ShouldBe(expected.Name);

        actual.Semester.ShouldBe(expected.Semester);
        actual.AcademicYear.ShouldBe(expected.AcademicYear);

        actual.CreatedAt.ShouldBe(expected.CreatedAt);

        if (actual is TimetableResponse actualFull && expected is TimetableResponse expectedFull)
            actualFull.MetaData.ShouldBe(expectedFull.MetaData);

        if (
            actual is TimetableDetailedResponse actualDetailed
            && expected is TimetableDetailedResponse expectedDetailed
        )
        {
            actualDetailed.Profile.ShouldBe(expectedDetailed.Profile);
        }
    }

    public static void ShouldMatch(this Timetable actual, Timetable expected)
    {
        actual.Id.ShouldBe(expected.Id);
        actual.Name.ShouldBe(expected.Name);

        actual.RoomId.ShouldBe(expected.RoomId);
        actual.UserId.ShouldBe(expected.UserId);

        actual.Semester.ShouldBe(expected.Semester);
        actual.AcademicYear.ShouldBe(expected.AcademicYear);
        actual.MetaData.ShouldBe(expected.MetaData);
    }

    public static void ShouldMatch(this RoomTimetable actual, RoomTimetable expected)
    {
        actual.Id.ShouldBe(expected.Id);
        actual.Name.ShouldBe(expected.Name);

        actual.RoomId.ShouldBe(expected.RoomId);
        actual.UserId.ShouldBe(expected.UserId);

        actual.Semester.ShouldBe(expected.Semester);
        actual.AcademicYear.ShouldBe(expected.AcademicYear);
        actual.OriginalTimetableId.ShouldBe(expected.OriginalTimetableId);

        actual.MetaData.ShouldBe(expected.MetaData);
    }

    public static void ShouldMatch<T>(
        this IEnumerable<T> actual,
        IEnumerable<T> expected,
        Func<T, Guid> idSelector,
        Action<T, T> matcher
    )
        where T : class
    {
        var actualList = actual.ToList();
        var expectedList = expected.ToList();

        actualList.Count.ShouldBe(expectedList.Count);

        foreach (var expectedItem in expectedList)
        {
            var actualItem = actualList.FirstOrDefault(a =>
                idSelector(a) == idSelector(expectedItem)
            );

            actualItem.ShouldNotBeNull(
                $"Expected item with id {idSelector(expectedItem)} was not found"
            );

            matcher(actualItem, expectedItem);
        }
    }

    public static void ShouldMatch(
        this IEnumerable<TimetableDetailedResponse> actual,
        IEnumerable<TimetableDetailedResponse> expected
    ) => actual.ShouldMatch(expected, t => t.Id, (a, e) => a.ShouldMatch(e));
}
