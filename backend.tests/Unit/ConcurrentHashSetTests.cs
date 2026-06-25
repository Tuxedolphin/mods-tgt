using Backend.Infrastructure;
using Shouldly;

namespace Backend.Tests.Unit;

public class ConcurrentHashSetTests
{
    // === Constructor ===

    [Fact]
    public void Constructor_Default_IsEmpty()
    {
        var set = new ConcurrentHashSet<int>();

        set.ShouldBeEmpty();
        set.Count.ShouldBe(0);
        set.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithItems_ContainsAllItems()
    {
        var items = new[] { 1, 2, 3 };

        var set = new ConcurrentHashSet<int>(items);

        set.Count.ShouldBe(3);
        items.ToList().ForEach(item => set.Contains(item).ShouldBeTrue());
    }

    [Fact]
    public void Constructor_WithNull_IsEmpty()
    {
        var set = new ConcurrentHashSet<int>(null);
        set.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyEnumerable_IsEmpty()
    {
        var set = new ConcurrentHashSet<int>([]);
        set.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithDuplicates_DeduplicatesItems()
    {
        var items = new[] { 1, 1, 2, 2, 3 };
        var set = new ConcurrentHashSet<int>(items);

        set.Count.ShouldBe(3);
    }

    // === Add ===

    [Fact]
    public void Add_NewItem_ReturnsTrueAndIncreasesCount()
    {
        var set = new ConcurrentHashSet<int>();

        var result = set.Add(1);

        result.ShouldBeTrue();
        set.Count.ShouldBe(1);
        set.Contains(1).ShouldBeTrue();
    }

    [Fact]
    public void Add_DuplicateItem_ReturnsFalseAndDoesNotIncreaseCount()
    {
        var set = new ConcurrentHashSet<int> { 1 };
        var result = set.Add(1);

        result.ShouldBeFalse();
        set.Count.ShouldBe(1);
    }

    // === Remove ===

    [Fact]
    public void Remove_ExistingItem_ReturnsTrueAndDecreasesCount()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        var result = set.Remove(1);

        result.ShouldBeTrue();
        set.Count.ShouldBe(2);
        set.Contains(1).ShouldBeFalse();
    }

    [Fact]
    public void Remove_NonExistentItem_ReturnsFalse()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        var result = set.Remove(99);

        result.ShouldBeFalse();
        set.Count.ShouldBe(3);
    }

    [Fact]
    public void Remove_FromEmptySet_ReturnsFalse()
    {
        var set = new ConcurrentHashSet<int>();

        var result = set.Remove(1);

        result.ShouldBeFalse();
    }

    // === Contains ===

    [Fact]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.Contains(2).ShouldBeTrue();
    }

    [Fact]
    public void Contains_NonExistentItem_ReturnsFalse()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.Contains(99).ShouldBeFalse();
    }

    [Fact]
    public void Contains_AfterRemoval_ReturnsFalse()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);
        set.Remove(2);

        set.Contains(2).ShouldBeFalse();
    }

    // === Clear ===

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.Clear();

        set.ShouldBeEmpty();
        set.Count.ShouldBe(0);
        set.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Clear_OnEmptySet_DoesNotThrow()
    {
        var set = new ConcurrentHashSet<int>();

        Action act = set.Clear;
        act.ShouldNotThrow();
    }

    // === IsEmpty ===

    [Fact]
    public void IsEmpty_AfterAddingAndRemovingAllItems_ReturnsTrue()
    {
        var set = new ConcurrentHashSet<int>();
        _ = set.Add(1);
        set.Remove(1);

        set.IsEmpty.ShouldBeTrue();
    }

    // === Items ===

    [Fact]
    public void Items_ReturnsAllItems()
    {
        var items = new[] { 1, 2, 3 };
        var set = new ConcurrentHashSet<int>(items);

        set.Items.ShouldBe(items, ignoreOrder: true);
    }

    [Fact]
    public void Items_OnEmptySet_ReturnsEmpty()
    {
        var set = new ConcurrentHashSet<int>();

        set.Items.ShouldBeEmpty();
    }

    // === Enumeration ===

    [Fact]
    public void Enumeration_YieldsAllItems()
    {
        var items = new[] { 1, 2, 3 };
        var set = new ConcurrentHashSet<int>(items);

        var enumerated = set.ToList();

        enumerated.ShouldBe(items, ignoreOrder: true);
    }

    [Fact]
    public void Enumeration_OnEmptySet_YieldsNothing()
    {
        var set = new ConcurrentHashSet<int>();

        set.ToList().ShouldBeEmpty();
    }
}
