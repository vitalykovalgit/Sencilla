namespace Sencilla.Core.Tests;

public class AccessTests
{
    [Fact]
    public void Nests_AndRestoresTheOuterScopeOnDispose()
    {
        Assert.Null(Access.Current);

        using (var outer = Access.Root())
        {
            Assert.Same(outer, Access.Current);
            using (var inner = Access.Root())
                Assert.Same(inner, Access.Current);
            Assert.Same(outer, Access.Current);
        }

        Assert.Null(Access.Current);
    }

    [Fact]
    public async Task ConcurrentFlows_DoNotSeeEachOther()
    {
        // Two handlers running side by side, as a consumer with MaxConcurrentHandlers > 1 does: each opens
        // and closes its own scope, and neither the sibling nor the parent flow may observe it.
        var gate = new TaskCompletionSource();
        var opened = Task.Run(async () =>
        {
            using var root = Access.Root();
            await gate.Task;
            Assert.True(Access.Current?.AllowAll);
        });
        var bare = Task.Run(async () =>
        {
            await Task.Yield();
            Assert.Null(Access.Current);
        });

        await bare;
        Assert.Null(Access.Current);
        gate.SetResult();
        await opened;
        Assert.Null(Access.Current);
    }
}
