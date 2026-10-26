using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using DeskGrid.Desktop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DeskGrid.Panel;

/// <summary>
/// The icon that follows the cursor while an item is dragged out of a panel: lifts off when the drag starts,
/// and flies to its slot or shrinks away when it ends. Never takes input or activation.
/// </summary>
internal sealed class DragGhost : Window
{
    private const double WindowSize = 96;
    private const double IconSize = 48;
    private const double TileCornerRadius = 10.7; // as the tile in PopupPanel.xaml
    private const double TilePadding = 5.8;
    private const double LiftedScale = 1.2;
    private static readonly TimeSpan LiftTime = TimeSpan.FromMilliseconds(140);
    private static readonly TimeSpan LandTime = TimeSpan.FromMilliseconds(180);

    private readonly ScaleTransform _scale = new(1, 1);
    private readonly Grid _content = new();
    private ScreenPoint _center;
    private DispatcherTimer? _animation;

    /// <param name="center">Where the icon's center first appears, in physical screen pixels.</param>
    /// <param name="tile">The plate under the icon of a folder, or <see langword="null"/> for a file.</param>
    public DragGhost(ImageSource? icon, ScreenPoint center, Brush? tile = null)
    {
        _center = center;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        Focusable = false;
        Width = WindowSize;
        Height = WindowSize;

        var image = new Image { Source = icon };
        _content.Children.Add(new Border
        {
            Width = IconSize,
            Height = IconSize,
            // A folder's preview sits on its tile, as in the panel's list.
            Background = tile,
            CornerRadius = new CornerRadius(TileCornerRadius),
            Padding = tile is null ? new Thickness(0) : new Thickness(TilePadding),
            Child = image,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = _scale,
            Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 3, Opacity = 0.45 },
        });
        RenderOptions.SetBitmapScalingMode(_content, BitmapScalingMode.HighQuality);
        Content = _content;
        SourceInitialized += (_, _) =>
        {
            // Layered, so it can't be hit; not activatable, so the panel keeps its place in the focus order.
            var window = new HWND(new WindowInteropHelper(this).Handle);
            var style = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(window, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            style |= WINDOW_EX_STYLE.WS_EX_TRANSPARENT | WINDOW_EX_STYLE.WS_EX_NOACTIVATE | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW;
            PInvoke.SetWindowLong(window, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)style);

            // In place before the window first appears, so it never shows up anywhere else for a moment.
            PlaceCenter(_center);
        };
    }

    /// <summary>The icon's size grows a little, as if picked up.</summary>
    public void Lift()
    {
        var from = _scale.ScaleX;
        Animate(LiftTime, t =>
        {
            var scale = from + (LiftedScale - from) * (1 - (1 - t) * (1 - t));
            (_scale.ScaleX, _scale.ScaleY) = (scale, scale);
        });
    }

    /// <summary>Puts the icon's center at <paramref name="center"/> (physical screen pixels), above everything else.</summary>
    public void PlaceCenter(ScreenPoint center)
    {
        _center = center;
        var window = new HWND(new WindowInteropHelper(this).Handle);
        var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        var size = (int)Math.Round(WindowSize * scale);
        PInvoke.SetWindowPos(window, new HWND(-1), center.X - size / 2, center.Y - size / 2, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    /// <summary>
    /// Ends the drag: flies to <paramref name="target"/> and settles to normal size, or, with
    /// <paramref name="vanish"/>, shrinks and fades where it is. Closes itself, then calls <paramref name="done"/>.
    /// </summary>
    public void Land(ScreenPoint target, bool vanish, Action? done = null)
    {
        var from = _center;
        var startScale = _scale.ScaleX;
        var endScale = vanish ? 0.4 : 1.0;
        Animate(LandTime, t =>
        {
            var eased = 1 - (1 - t) * (1 - t) * (1 - t);
            if (!vanish)
            {
                PlaceCenter(new ScreenPoint(
                    (int)Math.Round(from.X + (target.X - from.X) * eased), (int)Math.Round(from.Y + (target.Y - from.Y) * eased)));
            }

            var scale = startScale + (endScale - startScale) * eased;
            (_scale.ScaleX, _scale.ScaleY) = (scale, scale);
            Opacity = vanish ? 1 - eased : 1;
            if (t >= 1)
            {
                Close();
                done?.Invoke();
            }
        });
    }

    // Runs step(t) for t going from 0 to 1 over the given time. A new animation replaces the running one, so a drag
    // that ends before the lift is over doesn't have both fighting over the icon's size.
    private void Animate(TimeSpan time, Action<double> step)
    {
        _animation?.Stop();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var timer = _animation = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(8) };
        timer.Tick += (_, _) =>
        {
            var t = Math.Min(1, clock.Elapsed / time);
            if (t >= 1)
            {
                timer.Stop();
            }

            step(t);
        };
        timer.Start();
    }
}
