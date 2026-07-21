using Backend.Models;
using Backend.Services.Rooms;
using Shouldly;

namespace Backend.Tests.Unit;

public class ProfileTrackerTests
{
    private readonly ProfileTracker _tracker = new();

    // === SetUser ===

    [Fact]
    public void SetUser_SetUser_SetsCorrectly()
    {
        var profile = MakeProfile();
        _tracker.SetUser(profile);

        _tracker.TryGetUserById(profile.Id, out var user).ShouldBeTrue();
        user.ShouldBe(profile);
    }

    [Fact]
    public void SetUser_SetUser_UpdatedCorrectly()
    {
        var userId = Guid.NewGuid();

        _tracker.SetUser(MakeProfile(userId));

        var newUser = MakeProfile(userId, "other name");
        _tracker.SetUser(newUser);

        _tracker.TryGetUserById(userId, out var profile).ShouldBeTrue();
        profile.ShouldBe(newUser);
    }

    // === ClearAllUsers ===

    [Fact]
    public void ClearAllUsers_ClearAllOnEmpty_ResultsInNoChange()
    {
        _tracker.ClearAllUsers();
        _tracker.GetAllUsers().ShouldBeEmpty();
    }

    [Fact]
    public void ClearAllUsers_ClearAllOnSetUsers_ResultsInEmptyList()
    {
        IReadOnlyCollection<Profile> profiles = MakeProfileList();

        _tracker.SetUsers(profiles);
        _tracker.ClearAllUsers();

        _tracker.GetAllUsers().ShouldBeEmpty();
    }

    // === GetAllUsers ===

    [Fact]
    public void GetAllUsers_GetOnEmptyList_ReturnsEmpty()
    {
        _tracker.GetAllUsers().ShouldBeEmpty();
    }

    [Fact]
    public void GetAllUsers_GetOnSetUsers_ReturnsOriginalList()
    {
        IReadOnlyCollection<Profile> profiles = MakeProfileList();

        _tracker.SetUsers(profiles);
        _tracker.GetAllUsers().ShouldBe(profiles, ignoreOrder: true);
    }

    // === RemoveUser ===

    [Fact]
    public void RemoveUser_RemoveUserFromEmptyList_ReturnsFalse()
    {
        _tracker.RemoveUser(Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    public void RemoveUser_RemoveUserFromList_ReturnsTrueAndRemovesUser()
    {
        var profile = MakeProfile();

        _tracker.SetUser(profile);
        _tracker.RemoveUser(profile.Id).ShouldBeTrue();

        _tracker.GetAllUsers().ShouldBeEmpty();
    }

    // === RemoveUsers ===

    [Fact]
    public void RemoveUsers_RemoveFromEmptyList_ReturnsFalse()
    {
        _tracker.RemoveUsers(MakeProfileList().ConvertAll(p => p.Id)).ShouldBeFalse();
    }

    [Fact]
    public void RemoveUsers_RemoveNonExistentUsers_ReturnsFalse()
    {
        _tracker.SetUsers(MakeProfileList());
        _tracker.RemoveUsers(MakeProfileList().ConvertAll(p => p.Id)).ShouldBeFalse();
    }

    [Fact]
    public void RemoveUsers_RemoveExistingUsers_ReturnsTrueAndRemovesCorrectly()
    {
        var profiles = MakeProfileList();

        _tracker.SetUsers(profiles);
        _tracker.RemoveUsers(profiles.Slice(1, 2).ConvertAll(p => p.Id)).ShouldBeTrue();

        _tracker.GetAllUsers().ShouldBe(profiles[..1], ignoreOrder: true);
    }

    [Fact]
    public void RemoveUsers_RemoveExistingAndNonExistingUsers_ReturnsFalseAndRemovesCorrectly()
    {
        var profiles = MakeProfileList();

        _tracker.SetUsers(profiles);
        var removedUsers = profiles.Slice(1, 2).Append(MakeProfile());

        _tracker.RemoveUsers(removedUsers.ToList().ConvertAll(p => p.Id)).ShouldBeFalse();

        _tracker.GetAllUsers().ShouldBe(profiles[..1], ignoreOrder: true);
    }

    // === SetUsers ===

    [Fact]
    public void SetUsers_SetEmptyList_DoesNotSetAnything()
    {
        _tracker.SetUsers([]);
        _tracker.GetAllUsers().ShouldBeEmpty();
    }

    [Fact]
    public void SetUsers_SetNonExistentUsers_SetsCorrectly()
    {
        var users = MakeProfileList();

        _tracker.SetUsers(users);
        _tracker.GetAllUsers().ShouldBe(users, ignoreOrder: true);
    }

    [Fact]
    public void SetUsers_SetExistingUsers_OverridesCorrectly()
    {
        var users = MakeProfileList();
        _tracker.SetUsers(users);

        var changedUsers = users[..2]
            .ConvertAll(p => new Profile() { Id = p.Id, Username = "This has been changed!" });

        _tracker.SetUsers(changedUsers);

        _tracker.GetAllUsers().ShouldBe(changedUsers.Append(users[2]), ignoreOrder: true);
    }

    private static Profile MakeProfile(Guid? id = null, string username = "Test") =>
        new() { Id = id ?? Guid.NewGuid(), Username = username };

    private static List<Profile> MakeProfileList(int number = 3) =>
        [.. Enumerable.Range(0, number).Select(_ => MakeProfile())];
}
