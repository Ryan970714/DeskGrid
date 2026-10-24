using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DeskFolders.Icons;

/// <summary>
/// Draws up to four icons into one transparent 2×2 image, filled row by row like a phone folder.
/// The folder background is the widget's job, not part of the image.
/// </summary>
internal static class PreviewComposer
{
    public const int Size = 256;
    public const int Slots = 4;

    private const double Gap = Size * 0.08;
    private const double Cell = (Size - Gap) / 2;

    public static Rect CellBounds(int slot) =>
        new(slot % 2 * (Cell + Gap), slot / 2 * (Cell + Gap), Cell, Cell);

    /// <summary>Returns a frozen image; <see langword="null"/> icons leave their cell empty.</summary>
    public static BitmapSource Compose(IReadOnlyList<BitmapSource?> icons)
    {
        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
        using (var context = visual.RenderOpen())
        {
            for (var slot = 0; slot < Math.Min(icons.Count, Slots); slot++)
            {
                if (icons[slot] is { } icon)
                {
                    context.DrawImage(icon, CellBounds(slot));
                }
            }
        }

        var bitmap = new RenderTargetBitmap(Size, Size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
