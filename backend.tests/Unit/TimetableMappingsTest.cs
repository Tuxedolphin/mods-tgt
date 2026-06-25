using Backend.DTOs;
using Backend.DTOs.Mappings;
using Backend.Models;
using Shouldly;

namespace Backend.Tests.Unit;

public class TimetableMappingsTests
{
    // === Helpers ===

    private static TimetableModule MakeModule() =>
        new()
        {
            ModuleCode = "CS2040S",
            LessonNo = "1",
            LessonType = "Lecture",
            Colour = "#ffffff",
        };

    private static Timetable MakeTimetable() =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Test Timetable",
            Semester = 1,
            AcademicYear = "2024/2025",
            CreatedAt = DateTime.UtcNow,
            MetaData = [MakeModule(), MakeModule()],
            RoomId = Guid.NewGuid(),
        };

    private static Profile MakeProfile() => new() { Id = Guid.NewGuid(), Username = "testuser" };

    private static UpdateTimetableRequest MakeUpdateRequest() =>
        new() { Name = "Updated Timetable", MetaData = [MakeModule(), MakeModule()] };

    // === ToResponse ===

    [Fact]
    public void ToResponse_MapsAllFieldsCorrectly()
    {
        var timetable = MakeTimetable();

        var result = timetable.ToResponse();

        result.Id.ShouldBe(timetable.Id);
        result.Name.ShouldBe(timetable.Name);
        result.Semester.ShouldBe(timetable.Semester);
        result.AcademicYear.ShouldBe(timetable.AcademicYear);
        result.CreatedAt.ShouldBe(timetable.CreatedAt);
        result.MetaData.ShouldBe(timetable.MetaData);
    }

    [Fact]
    public void ToResponse_MetaData_IsNewCollection_NotSameReference()
    {
        var timetable = MakeTimetable();

        var result = timetable.ToResponse();

        result.MetaData.ShouldNotBeSameAs(timetable.MetaData);
    }

    [Fact]
    public void ToResponse_MetaData_ContentsMatchSource()
    {
        var timetable = MakeTimetable();

        var result = timetable.ToResponse();

        result.MetaData.ShouldBe(timetable.MetaData);
    }

    // === ToSummaryResponse ===

    [Fact]
    public void ToSummaryResponse_MapsAllFieldsCorrectly()
    {
        var timetable = MakeTimetable();

        var result = timetable.ToSummaryResponse();

        result.Id.ShouldBe(timetable.Id);
        result.Name.ShouldBe(timetable.Name);
        result.Semester.ShouldBe(timetable.Semester);
        result.AcademicYear.ShouldBe(timetable.AcademicYear);
        result.CreatedAt.ShouldBe(timetable.CreatedAt);
    }

    // === ToDetailedResponse ===

    [Fact]
    public void ToDetailedResponse_MapsAllFieldsCorrectly()
    {
        var timetable = MakeTimetable();
        var profile = MakeProfile();

        var result = timetable.ToDetailedResponse(profile);

        result.Id.ShouldBe(timetable.Id);
        result.Name.ShouldBe(timetable.Name);
        result.Profile.ShouldBe(profile);
        result.Semester.ShouldBe(timetable.Semester);
        result.AcademicYear.ShouldBe(timetable.AcademicYear);
        result.CreatedAt.ShouldBe(timetable.CreatedAt);
        result.MetaData.ShouldBe(timetable.MetaData);
    }

    [Fact]
    public void ApplyUpdate_MetaData_IsNewCollection_NotSameReferenceAsRequest()
    {
        var timetable = MakeTimetable();
        var requestMetaData = new List<TimetableModule> { MakeModule() };
        var request = new UpdateTimetableRequest { Name = "Name", MetaData = requestMetaData };

        timetable.ApplyUpdate(request);

        timetable.MetaData.ShouldNotBeSameAs(requestMetaData);
        timetable.MetaData.ShouldBe(requestMetaData); // contents still match
    }

    [Fact]
    public void ToDetailedResponse_ProfileIsSameInstance()
    {
        var timetable = MakeTimetable();
        var profile = MakeProfile();

        var result = timetable.ToDetailedResponse(profile);

        result.Profile.ShouldBeSameAs(profile);
    }

    // === ApplyUpdate ===

    [Fact]
    public void ApplyUpdate_MutatesNameCorrectly()
    {
        var timetable = MakeTimetable();
        var request = MakeUpdateRequest();

        timetable.ApplyUpdate(request);

        timetable.Name.ShouldBe(request.Name);
    }

    [Fact]
    public void ApplyUpdate_MutatesMetaDataCorrectly()
    {
        var timetable = MakeTimetable();
        var request = MakeUpdateRequest();

        timetable.ApplyUpdate(request);

        timetable.MetaData.ShouldBe(request.MetaData);
    }

    [Fact]
    public void ApplyUpdate_MetaData_IsShallowCopy()
    {
        var timetable = MakeTimetable();
        var requestMetaData = new List<TimetableModule> { MakeModule() };
        var request = new UpdateTimetableRequest { Name = "Name", MetaData = requestMetaData };

        timetable.ApplyUpdate(request);

        var originalMetaData = timetable.MetaData;
        requestMetaData.Clear();
        timetable.MetaData.ShouldBe(originalMetaData);
    }

    [Fact]
    public void ApplyUpdate_ReturnsSameTimetableInstance()
    {
        var timetable = MakeTimetable();
        var request = MakeUpdateRequest();

        var result = timetable.ApplyUpdate(request);

        result.ShouldBeSameAs(timetable);
    }

    [Fact]
    public void ApplyUpdate_DoesNotMutateUnrelatedFields()
    {
        var timetable = MakeTimetable();
        var originalId = timetable.Id;
        var originalUserId = timetable.UserId;
        var originalRoomId = timetable.RoomId;
        var originalCreatedAt = timetable.CreatedAt;
        var originalSemester = timetable.Semester;
        var originalAcademicYear = timetable.AcademicYear;

        timetable.ApplyUpdate(MakeUpdateRequest());

        timetable.Id.ShouldBe(originalId);
        timetable.UserId.ShouldBe(originalUserId);
        timetable.RoomId.ShouldBe(originalRoomId);
        timetable.CreatedAt.ShouldBe(originalCreatedAt);
        timetable.Semester.ShouldBe(originalSemester);
        timetable.AcademicYear.ShouldBe(originalAcademicYear);
    }
}
