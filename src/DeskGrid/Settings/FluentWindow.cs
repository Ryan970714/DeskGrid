using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using DeskGrid.Panel;
using Windows.Win32.Foundation;

namespace DeskGrid.Settings;

/// <summary>What the windows with the Fluent theme (<c>ThemeMode="System"</c>) share.</summary>
internal static class FluentWindow
{
    /// <summary>
    /// The Fluent theme gives a window a Mica backdrop, which makes WPF composite its text with alpha
    /// (grayscale antialiasing, visibly softer than ClearType). A plain opaque window keeps the text sharp.
    /// Call before the window is first shown.
    /// </summary>
    public static void UseOpaqueBackground(Window window) => window.SourceInitialized += (_, _) =>
    {
        var dark = PanelTheme.SystemUsesDarkTheme;
        Backdrop.DisableBackdrop(new HWND(new WindowInteropHelper(window).Handle));
        window.Background = new SolidColorBrush(dark ? Color.FromRgb(0x20, 0x20, 0x20) : Color.FromRgb(0xF3, 0xF3, 0xF3));
    };
}
