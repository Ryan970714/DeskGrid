using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DeskFolders.Desktop;
using DeskFolders.Diagnostics;
using DeskFolders.Folders;
using DeskFolders.Icons;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DeskFolders.Spike;

/// <summary>
/// Placeholder content until the real widgets exist (stage 4). Rounded corners prove per-pixel
/// transparency, left-click cycles the color, the counter increments on every re-attach, and each
/// scanned folder shows its 2×2 preview. Right-click exits.
/// </summary>
internal sealed class SpikeTile : Border
{
    private const double MinSize = 128;
    private const double PreviewSize = 72;
    private static readonly Brush[] Palette = [Brushes.DodgerBlue, Brushes.MediumSeaGreen, Brushes.Tomato];
    private static readonly Brush FolderBackground = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF));

    private readonly TextBlock _header = new() { FontSize = 14, TextAlignment = TextAlignment.Center };
    private readonly WrapPanel _widgets = new() { Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
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

    public void ShowWidgets(IReadOnlyList<WidgetFolder> widgets, IconService icons)
    {
        _widgets.Children.Clear();
        foreach (var widget in widgets)
        {
            var preview = new Image { Width = PreviewSize, Height = PreviewSize };
            RenderOptions.SetBitmapScalingMode(preview, BitmapScalingMode.HighQuality);
            _widgets.Children.Add(new StackPanel
            {
                Margin = new Thickness(6),
                Children =
                {
                    new Border { Background = FolderBackground, CornerRadius = new CornerRadius(16), Padding = new Thickness(8), Child = preview },
                    new TextBlock { Text = $"{widget.Name} ({widget.Items.Count})", FontSize = 12, TextAlignment = TextAlignment.Center },
                },
            });
            _ = LoadPreviewAsync(preview, icons.GetPreviewAsync(widget));
        }
    }

    /// <summary>Position that puts the tile's top-left corner near the center of the primary monitor.</summary>
    public static ScreenPoint CenterOfPrimaryScreen()
    {
        var halfTile = (int)(MinSize / 2 * PInvoke.GetDpiForSystem() / 96);
        return new ScreenPoint(
            PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN) / 2 - halfTile,
            PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN) / 2 - halfTile);
    }

    private static async Task LoadPreviewAsync(Image target, Task<System.Windows.Media.Imaging.BitmapSource> preview)
    {
        try
        {
            target.Source = await preview;
        }
        catch (Exception exception)
        {
            Log.Error("Preview failed", exception);
        }
    }
}
