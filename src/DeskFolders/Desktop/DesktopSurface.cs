using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DeskFolders.Desktop;

/// <summary>
/// A per-pixel transparent child window that hosts WPF content inside Explorer's desktop icon layer.
/// <see cref="DesktopHost"/> decides when to (re)attach it.
/// </summary>
internal sealed class DesktopSurface(UIElement content, ScreenPoint position) : IDisposable
{
    // Win32 HWND_TOP; not in CsWin32's metadata.
    private static readonly HWND HwndTop = HWND.Null;

    private HwndSource? _source;

    /// <summary>Top-left corner in physical screen pixels.</summary>
    public ScreenPoint Position { get; } = position;

    /// <summary>Raised when someone else destroys the window, typically Explorer exiting.</summary>
    public event EventHandler? Lost;

    public bool IsAttachedTo(HWND layer) =>
        _source is { IsDisposed: false } source && PInvoke.GetParent(new HWND(source.Handle)) == layer;

    /// <summary>Creates a fresh child window under <paramref name="layer"/>, replacing any previous one.</summary>
    public void AttachTo(HWND layer)
    {
        Release();

        var origin = new System.Drawing.Point(Position.X, Position.Y);
        PInvoke.ScreenToClient(layer, ref origin);

        var parameters = new HwndSourceParameters(nameof(DesktopSurface))
        {
            ParentWindow = layer,
            WindowStyle = (int)(WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_CLIPSIBLINGS),
            // Explorer's thread must never be able to block ours through parent notifications.
            ExtendedWindowStyle = (int)WINDOW_EX_STYLE.WS_EX_NOPARENTNOTIFY,
            // Layered child window: the only kind that renders under the composition-based Progman
            // of 24H2+, and fully transparent pixels become click-through.
            UsesPerPixelTransparency = true,
            PositionX = origin.X,
            PositionY = origin.Y,
        };
        _source = new HwndSource(parameters) { SizeToContent = SizeToContent.WidthAndHeight, RootVisual = content };
        _source.Disposed += OnSourceDisposed;

        // Above the icon list view, so clicks land on us rather than on the icons.
        PInvoke.SetWindowPos(new HWND(_source.Handle), HwndTop, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    public void Dispose() => Release();

    private void Release()
    {
        if (_source is null)
        {
            return;
        }

        _source.Disposed -= OnSourceDisposed;
        _source.Dispose();
        _source = null;
    }

    private void OnSourceDisposed(object? sender, EventArgs e)
    {
        _source = null;
        Lost?.Invoke(this, EventArgs.Empty);
    }
}
