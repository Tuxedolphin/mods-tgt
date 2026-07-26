using System.Text.Json;
using Backend.Services.NusMods;
using Shouldly;

namespace Backend.Tests.Unit;

public class NusModsParsingTests
{
    [Theory]
    [InlineData("0800", 480)]
    [InlineData("1430", 870)]
    [InlineData("0000", 0)]
    public void ParseTimeToMinutes_ValidInput_ReturnsMinutes(string input, int expected) =>
        NusModsParsing.ParseTimeToMinutes(input).ShouldBe(expected);

    [Fact]
    public void ParseWeeks_NumericArray_ReturnsSet()
    {
        using var doc = JsonDocument.Parse("[1,2,3,7,13]");
        NusModsParsing.ParseWeeks(doc.RootElement).ShouldBe(new[] { 1, 2, 3, 7, 13 }, ignoreOrder: true);
    }

    [Fact]
    public void ParseWeeks_RangeObjectWithWeeks_ReturnsListedWeeks()
    {
        using var doc = JsonDocument.Parse("""{"start":"2026-08-10","end":"2026-11-13","weeks":[2,4,6]}""");
        NusModsParsing.ParseWeeks(doc.RootElement).ShouldBe(new[] { 2, 4, 6 }, ignoreOrder: true);
    }

    [Fact]
    public void ParseWeeks_RangeObjectWithoutWeeks_FallsBackToAllTeachingWeeks()
    {
        using var doc = JsonDocument.Parse("""{"start":"2026-08-10","end":"2026-11-13"}""");
        NusModsParsing.ParseWeeks(doc.RootElement).ShouldBe(Enumerable.Range(1, 13), ignoreOrder: true);
    }

    [Fact]
    public void ParseModule_ValidDocument_ReturnsSessionsForRequestedSemester()
    {
        using var doc = JsonDocument.Parse("""
        {
          "moduleCode": "CS2103T",
          "semesterData": [
            { "semester": 1, "timetable": [
                { "classNo": "G12", "lessonType": "Lecture", "day": "Thursday",
                  "startTime": "0800", "endTime": "0900", "weeks": [1,2,3] } ] },
            { "semester": 2, "timetable": [] }
          ]
        }
        """);

        var result = NusModsParsing.ParseModule("CS2103T", 1, doc);

        var session = result.Sessions.ShouldHaveSingleItem();
        session.ClassNo.ShouldBe("G12");
        session.LessonType.ShouldBe("Lecture");
        session.Day.ShouldBe(WeekDay.Thursday);
        session.StartMin.ShouldBe(480);
        session.EndMin.ShouldBe(540);
        session.Weeks.ShouldBe(new[] { 1, 2, 3 }, ignoreOrder: true);
    }

    [Fact]
    public void ParseModule_SemesterMissing_ReturnsEmptySessions()
    {
        using var doc = JsonDocument.Parse("""{ "moduleCode": "CS2103T", "semesterData": [] }""");
        NusModsParsing.ParseModule("CS2103T", 1, doc).Sessions.ShouldBeEmpty();
    }
}
