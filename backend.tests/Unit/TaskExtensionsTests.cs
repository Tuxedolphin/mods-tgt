using Backend.Infrastructure;
using Shouldly;

namespace Backend.Tests.Unit;

public class TaskExtensionsTests
{
    // === MapAsync ===

    [Fact]
    public async Task MapAsync_AppliesTransformationCorrectly()
    {
        Task<int> task = Task.FromResult(5);

        int result = await task.MapAsync(x => x * 2);

        result.ShouldBe(10);
    }

    [Fact]
    public async Task MapAsync_WorksWithTypeTransformation()
    {
        Task<int> task = Task.FromResult(42);

        string result = await task.MapAsync(x => x.ToString());

        result.ShouldBe("42");
    }

    [Fact]
    public async Task MapAsync_WorksWithIdentityTransformation()
    {
        Task<int> task = Task.FromResult(7);

        int result = await task.MapAsync(x => x);

        result.ShouldBe(7);
    }

    [Fact]
    public async Task MapAsync_AwaitsTaskBeforeApplyingTransformation()
    {
        Task<int> task = Task.Run(async () =>
        {
            await Task.Delay(10);
            return 3;
        });

        int result = await task.MapAsync(x => x + 1);

        result.ShouldBe(4);
    }

    [Fact]
    public async Task MapAsync_FunctionThrowingException_PropagatesException()
    {
        Task<int> task = Task.FromResult(1);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            task.MapAsync<int, int>(_ => throw new InvalidOperationException())
        );
    }

    [Fact]
    public async Task MapAsync_FaultedTask_PropagatesException()
    {
        Task<int> faultedTask = Task.FromException<int>(new InvalidOperationException());

        await Should.ThrowAsync<InvalidOperationException>(() => faultedTask.MapAsync(x => x * 2));
    }
}
