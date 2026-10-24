using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DeskFolders.Desktop;
using DeskFolders.Folders;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DeskFolders.Spike;

/// <summary>
/// Placeholder content until the real widgets exist (stage 4). Rounded corners prove per-pixel
/// transparency, left-click cycles the color, the counter increments on every re-attach, and the
/// list mirrors the scanned folders with items in saved order. Right-click exits.
/// </summary>
internal sealed class SpikeTile : Border
{
    private const double MinSize = 128;
    private static readonly Brush[] Palette = [Brushes.DodgerBlue, Brushes.MediumSeaGreen, Brushes.Tomato];

    private readonly TextBlock _header = new() { FontSize = 14, TextAlignment = TextAlignment.Center };
    private readonly TextBlock _widgets = new() { FontSize = 12, Margin = new Thickness(0, 8, 0, 0) };
    private int _attachCount;
    private int _colorIndex;

    public SpikeTile()
    {
        MinWidth = MinSize;
        MinHeight = MinSize;
        MaxWidth = 480;
        Padding = new Thickness(16);
        CornerRadius = new CornerRadius(24);
        Background = Palette[0];
        TextElement.SetForeground(this, Brushes.White);
        Child = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { _header, _widgets } };

        // Loaded fires each time the tile is hosted by a new window.
        Loaded += (_, _) => _header.Text = $"DeskFolders\nattach #{++_attachCount}";
        MouseLeftButtonUp += (_, _) => Background = Palette[_colorIndex = (_colorIndex + 1) % Palette.Length];
        MouseRightButtonUp += (_, _) => Application.Current.Shutdown();
    }

    public void ShowWidgets(IReadOnlyList<WidgetFolder> widgets) =>
        _widgets.Text = widgets.Count == 0
            ? "(no folders)"
            : string.Join("\n", widgets.Select(w => $"{w.Name} ({w.Items.Count}): {string.Join(", ", w.Items.Select(i => i.Name))}"));

    /// <summary>Position that puts the tile's top-left corner near the center of the primary monitor.</summary>
    public static ScreenPoint CenterOfPrimaryScreen()
    {
        var halfTile = (int)(MinSize / 2 * PInvoke.GetDpiForSystem() / 96);
        return new ScreenPoint(
            PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN) / 2 - halfTile,
            PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN) / 2 - halfTile);
    }
}
