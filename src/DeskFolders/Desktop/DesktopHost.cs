using System.ComponentModel;
using System.Windows.Interop;
using System.Windows.Threading;
using DeskFolders.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DeskFolders.Desktop;

/// <summary>
/// Keeps <see cref="DesktopSurface"/>s attached to Explorer's desktop icon layer and re-attaches
/// them whenever Explorer recreates the desktop (explorer.exe restart or crash).
/// </summary>
internal sealed class DesktopHost : IDisposable
{
    private readonly List<DesktopSurface> _surfaces = [];
    private readonly DispatcherTimer _retryTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly uint _taskbarCreatedMessage = PInvoke.RegisterWindowMessage("TaskbarCreated");
    private HwndSource? _shellEventsWindow;
    private IconLayer? _layer;

    public DesktopHost() => _retryTimer.Tick += (_, _) => EnsureAttached();

    /// <summary>Starts managing <paramref name="surface"/>; attaches it right away once started.</summary>
    public void Add(DesktopSurface surface)
    {
        _surfaces.Add(surface);
        surface.Lost += OnSurfaceLost;

        if (_layer is { } layer && PInvoke.IsWindow(layer.Handle))
        {
            TryAttach(surface, layer);
        }
        else if (_shellEventsWindow is not null)
        {
            EnsureAttached();
        }
    }

    /// <summary>Stops managing <paramref name="surface"/> and destroys its window.</summary>
    public void Remove(DesktopSurface surface)
    {
        if (!_surfaces.Remove(surface))
        {
            return;
        }

        surface.Lost -= OnSurfaceLost;
        surface.Dispose();
    }

    public void Start()
    {
        _shellEventsWindow = CreateShellEventsWindow();
        EnsureAttached();
    }

    public void Dispose()
    {
        _retryTimer.Stop();
        foreach (var surface in _surfaces)
        {
            surface.Lost -= OnSurfaceLost;
            surface.Dispose();
        }

        _surfaces.Clear();
        _shellEventsWindow?.Dispose();
    }

    private void EnsureAttached()
    {
        if (IconLayer.Find() is not { } layer)
        {
            if (!_retryTimer.IsEnabled)
            {
                Log.Info("Desktop icon layer not found; waiting for Explorer.");
                _retryTimer.Start();
            }

            return;
        }

        _retryTimer.Stop();
        _layer = layer;
        var attached = _surfaces.Where(s => !s.IsAttachedTo(layer.Handle)).ToList().Count(s => TryAttach(s, layer));
        if (attached > 0)
        {
            Log.Info($"Attached {attached} surface(s) to SHELLDLL_DefView 0x{(nint)layer.Handle:X} ({layer.Layout} layout).");
        }
    }

    private bool TryAttach(DesktopSurface surface, IconLayer layer)
    {
        try
        {
            surface.AttachTo(layer.Handle);
            return true;
        }
        catch (Win32Exception exception)
        {
            // The layer went away between finding it and creating the window.
            Log.Error("Attaching a surface failed; retrying", exception);
            _retryTimer.Start();
            return false;
        }
    }

    private void OnSurfaceLost(object? sender, EventArgs e)
    {
        // We are inside the window's teardown and Explorer may be half gone: retry later instead.
        Log.Info("Surface window destroyed externally; re-attaching once Explorer is back.");
        _retryTimer.Start();
    }

    private unsafe HwndSource CreateShellEventsWindow()
    {
        // Hidden, unowned top-level window: message-only windows don't receive broadcasts.
        var source = new HwndSource(new HwndSourceParameters("DeskFolders.ShellEvents")
        {
            WindowStyle = unchecked((int)WINDOW_STYLE.WS_POPUP),
            Width = 0,
            Height = 0,
        });

        // Let Explorer's broadcast through UIPI if we happen to run elevated.
        PInvoke.ChangeWindowMessageFilterEx(new HWND(source.Handle), _taskbarCreatedMessage, WINDOW_MESSAGE_FILTER_ACTION.MSGFLT_ALLOW, null);
        source.AddHook(ShellEventsHook);
        return source;
    }

    private IntPtr ShellEventsHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // Explorer broadcasts this after (re)creating the taskbar, e.g. when explorer.exe restarts.
        if ((uint)msg == _taskbarCreatedMessage)
        {
            Log.Info("TaskbarCreated received.");
            EnsureAttached();
        }

        return IntPtr.Zero;
    }
}
