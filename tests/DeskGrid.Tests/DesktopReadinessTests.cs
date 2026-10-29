using DeskGrid.Desktop;

namespace DeskGrid.Tests;

public sealed class DesktopReadinessTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(5);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task WaitUntilSettled_ReturnsOnceTwoPollsAgree()
    {
        string?[] polls = [null, "0,0", "0,0;150,0", "0,0;150,0", "never"];
        var next = 0;

        var settled = await DesktopReadiness.WaitUntilSettledAsync(() => polls[next++], Interval, Timeout, CancellationToken.None);

        Assert.True(settled);
        Assert.Equal(4, next); // The poll after the agreeing pair is never made.
    }

    [Fact]
    public async Task WaitUntilSettled_IconsThatKeepMoving_NeverSettle()
    {
        var next = 0;

        var settled = await DesktopReadiness.WaitUntilSettledAsync(
            () => $"{next++},0", Interval, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        Assert.False(settled);
    }

    [Fact]
    public async Task WaitUntilSettled_NoIconsAtAll_GivesUpAtTheTimeout()
    {
        var settled = await DesktopReadiness.WaitUntilSettledAsync(
            () => null, Interval, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        Assert.False(settled);
    }

    [Fact]
    public async Task WaitUntilSettled_StopsWhenCancelled()
    {
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            DesktopReadiness.WaitUntilSettledAsync(() => null, Interval, Timeout, cancel.Token));
    }
}
