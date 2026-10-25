using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeskGrid.Shell;

namespace DeskGrid.Taskbar;

/// <summary>
/// Creates the shortcut that, once pinned to the taskbar, opens a widget folder's panel. Windows lets only
/// the user pin, so the shortcut is created in a folder of ours and shown to them.
/// </summary>
internal static class PinShortcut
{
    private const int IconSize = 256;
    private const double TileCornerRadius = 57;

    private static readonly Brush TileBrush = new SolidColorBrush(Color.FromArgb(0xB0, 0x40, 0x40, 0x40));

    public static string FolderPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskGrid", "Pins");

    /// <summary>
    /// Writes <paramref name="folderName"/>.lnk (and its icon, drawn from <paramref name="preview"/>) and returns its path.
    /// Call on the UI thread.
    /// </summary>
    public static string Create(string folderName, BitmapSource preview)
    {
        Directory.CreateDirectory(FolderPath);
        var iconPath = Path.Combine(FolderPath, folderName + ".ico");
        var shortcutPath = Path.Combine(FolderPath, folderName + ".lnk");

        File.WriteAllBytes(iconPath, ToIcon(preview));
        ShortcutFile.Create(shortcutPath, Environment.ProcessPath!, OpenRequest.Arguments(folderName), iconPath);
        return shortcutPath;
    }

    // The preview on the same kind of tile the widget has, as a single-image (PNG) .ico.
    private static byte[] ToIcon(BitmapSource preview)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRoundedRectangle(TileBrush, null, new Rect(0, 0, IconSize, IconSize), TileCornerRadius, TileCornerRadius);
            context.DrawImage(preview, new Rect(0, 0, IconSize, IconSize));
        }

        var bitmap = new RenderTargetBitmap(IconSize, IconSize, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var png = new MemoryStream();
        encoder.Save(png);

        using var icon = new MemoryStream();
        using (var writer = new BinaryWriter(icon, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((ushort)0);      // reserved
            writer.Write((ushort)1);      // type: icon
            writer.Write((ushort)1);      // image count
            writer.Write((byte)0);        // width: 0 means 256
            writer.Write((byte)0);        // height: 0 means 256
            writer.Write((byte)0);        // palette size
            writer.Write((byte)0);        // reserved
            writer.Write((ushort)1);      // color planes
            writer.Write((ushort)32);     // bits per pixel
            writer.Write((uint)png.Length);
            writer.Write((uint)(6 + 16)); // image offset: after the header and its one entry
            writer.Write(png.ToArray());
        }

        return icon.ToArray();
    }
}
