using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DeskFolders.Desktop;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DeskFolders.Spike;

/// <summary>
/// Stage 1 placeholder content. Rounded corners prove per-pixel transparency, left-click cycles the
/// color (input works), the counter increments on every re-attach, right-click exits.
/// </summary>
internal sealed class SpikeTile : Border
{
    private const double Size = 128;
    private static readonly Brush[] Palette = [Brushes.DodgerBlue, Brushes.MediumSeaGreen, Brushes.Tomato];

    private readonly TextBlock _label = new()
    {
        Foreground = Brushes.White,
        FontSize = 14,
        TextAlignment = TextAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private int _attachCount;
    private int _colorIndex;

    public SpikeTile()
    {
        Width = Size;
        Height = Size;
        CornerRadius = new CornerRadius(24);
        Background = Palette[0];
        Child = _label;

        // Loaded fires each time the tile is hosted by a new window.
        Loaded += (_, _) => _label.Text = $"DeskFolders\nattach #{++_attachCount}";
        MouseLeftButtonUp += (_, _) => Background = Palette[_colorIndex = (_colorIndex + 1) % Palette.Length];
        MouseRightButtonUp += (_, _) => Application.Current.Shutdown();
    }

    /// <summary>Position that centers the tile on the primary monitor.</summary>
    public static ScreenPoint CenterOfPrimaryScreen()
    {
        var halfTile = (int)(Size / 2 * PInvoke.GetDpiForSystem() / 96);
        return new ScreenPoint(
            PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN) / 2 - halfTile,
            PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN) / 2 - halfTile);
    }
}
