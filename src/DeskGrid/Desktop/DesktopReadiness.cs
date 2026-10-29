using System.Diagnostics;

namespace DeskGrid.Desktop;

/// <summary>
/// Waits for Explorer to finish building the desktop. At sign-in the icon view exists well before the icons are in
/// it and laid out where the user left them; widgets placed in between would line up with a half-built grid.
/// </summary>
internal static class DesktopReadiness
{
    /// <summary>
    /// Polls <paramref name="snapshot"/> (a description of where the desktop icons are now, or <see langword="null"/>
    /// while there are none) until two polls in a row agree.
    /// </summary>
    /// <returns><see langword="false"/> if that did not happen within <paramref name="timeout"/>, e.g. on a desktop without icons.</returns>
    public static async Task<bool> WaitUntilSettledAsync(
        Func<string?> snapshot, TimeSpan interval, TimeSpan timeout, CancellationToken cancel)
    {
        var clock = Stopwatch.StartNew();
        string? previous = null;
        while (true)
        {
            // Reading the icons asks Explorer, which may be busy: not on the UI thread.
            var current = await Task.Run(snapshot, cancel);
            if (current is not null && current == previous)
            {
                return true;
            }

            previous = current;
            if (clock.Elapsed + interval >= timeout)
            {
                return false;
            }

            await Task.Delay(interval, cancel);
        }
    }
}
