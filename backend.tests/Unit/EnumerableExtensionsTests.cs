using Backend.Infrastructure;
using Shouldly;

namespace Backend.Tests.Unit;

public class EnumerableExtensionsTests
{
    private const int DEFAULT_SIZE = 3;

    // === ForEachAsync ===

    [Fact]
    public async Task ForEachAsync_ActionCalledForEveryElementAsync()
    {
        int counter = 0;

        await MakeTestList().ForEachAsync(async _ => counter++);
        counter.ShouldBe(DEFAULT_SIZE);
    }

    [Fact]
    public async Task ForEachAsync_ActionExecutedInOrderAsync()
    {
        List<int> called = [];
        await MakeTestList().ForEachAsync(async item => called.Add(item));

        called.ShouldBe(MakeTestList());
    }

    [Fact]
    public async Task ForEachAsync_ActionExecutedOnEmptyListDoesNothing()
    {
        int counter = 0;
        await Array.Empty<int>().ForEachAsync(async _ => counter++);

        counter.ShouldBe(0);
    }

    [Fact]
    public async Task ForEachAsync_ActionThrowingException_SuccessfullyDoesThingsBefore_DoesNotDoThingsAfter()
    {
        int counter = 0;

        await Should.ThrowAsync<InvalidOperationException>(() =>
            MakeTestList()
                .ForEachAsync(async i =>
                {
                    counter++;
                    if (i == 1)
                        throw new InvalidOperationException();
                })
        );

        counter.ShouldBe(1);
    }

    // === SelectAsync ===

    [Fact]
    public async Task SelectAsync_ActionCalledForEveryElement()
    {
        int counter = 0;

        await MakeTestList().SelectAsync(async _ => counter++);
        counter.ShouldBe(DEFAULT_SIZE);
    }

    [Fact]
    public async Task SelectAsync_ActionExecutedInOrder_ReturnsCorrectList()
    {
        List<int> called = [];
        var res = await MakeTestList()
            .SelectAsync(async item =>
            {
                called.Add(item);
                return item;
            });

        called.ShouldBe(MakeTestList());
        res.ShouldBe(MakeTestList());
    }

    [Fact]
    public async Task SelectAsync_ActionExecutedOnEmptyListDoesNothing()
    {
        int counter = 0;
        var res = await Array.Empty<int>().SelectAsync(async _ => counter++);

        counter.ShouldBe(0);
        res.ShouldBeEmpty();
    }

    [Fact]
    public async Task SelectAsync_ActionThrowingException_SuccessfullyDoesThingsBefore_DoesNotDoThingsAfter()
    {
        int counter = 0;

        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await MakeTestList()
                .SelectAsync(async i =>
                {
                    counter++;
                    if (i == 1)
                        throw new InvalidOperationException();
                    return i;
                });
        });

        counter.ShouldBe(1);
    }

    [Fact]
    public async Task SelectAsync_TransformsEachElement_ReturnsCorrectResults()
    {
        var result = await MakeTestList().SelectAsync(item => Task.FromResult(item * 2));
        result.ShouldBe([2, 4, 6]);
    }

    // === SelectAsync ===
    private static List<int> MakeTestList(int size = DEFAULT_SIZE) =>
        [.. Enumerable.Range(1, size)];
}
