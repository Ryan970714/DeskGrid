using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace DeskGrid.Desktop;

/// <summary>
/// Tells when desktop icons may have been added, removed or moved, from the accessibility events of the
/// desktop's thread in Explorer (no polling); a burst of them raises <see cref="Changed"/> once.
/// </summary>
/// <remarks>
/// The icon list view reports added and removed icons, but not moved ones (measured): those are caught by
/// the end of the mouse capture that every drag on the desktop holds.
/// </remarks>
internal sealed class DesktopIconWatcher : IDisposable
{
    // WinEvents (winuser.h).
    private const uint CaptureEnd = 0x0009;
    private const uint ObjectCreate = 0x8000;
    private const uint ObjectDestroy = 0x8001;
    private const uint ObjectReorder = 0x8004;
    private const uint ObjectLocationChange = 0x800B;
    private const uint OutOfContext = 0; // WINEVENT_OUTOFCONTEXT

    private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(300);

    private readonly DispatcherTimer _settle = new() { Interval = SettleDelay };
    private readonly WINEVENTPROC _callback; // Kept alive for as long as the hook is.
    private HWINEVENTHOOK _hook;
    private HWND _listView;

    public DesktopIconWatcher()
    {
        _callback = OnEvent;
        _settle.Tick += (_, _) =>
        {
            _settle.Stop();
            Changed?.Invoke(this, EventArgs.Empty);
        };
    }

    public event EventHandler? Changed;

    /// <summary>Follows <paramref name="listView"/> (Explorer's <c>SysListView32</c>) from now on.</summary>
    public void Watch(HWND listView)
    {
        if (listView == _listView)
        {
            return;
        }

        Unhook();
        _listView = listView;
        var thread = PInvoke.GetWindowThreadProcessId(listView, out var processId);
        if (listView.IsNull || thread == 0)
        {
            return;
        }

        // Only the desktop's own thread, not the taskbar or Explorer windows. Out of context: the events
        // are posted to this (UI) thread's message loop.
        _hook = PInvoke.SetWinEventHook(CaptureEnd, ObjectLocationChange, HMODULE.Null, _callback, processId, thread, OutOfContext);
    }

    public void Dispose()
    {
        _settle.Stop();
        Unhook();
    }

    private void Unhook()
    {
        if (!_hook.IsNull)
        {
            PInvoke.UnhookWinEvent(_hook);
            _hook = default;
        }
    }

    private void OnEvent(HWINEVENTHOOK hook, uint @event, HWND window, int objectId, int childId, uint thread, uint time)
    {
        // A drag's capture may belong to a helper window of the thread, so any window counts for that one.
        var relevant = @event == CaptureEnd
            || (window == _listView && @event is ObjectCreate or ObjectDestroy or ObjectReorder or ObjectLocationChange);
        if (!relevant)
        {
            return;
        }

        _settle.Stop();
        _settle.Start();
    }
}
