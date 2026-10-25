using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeskGrid.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;

namespace DeskGrid.Shell;

/// <summary>Asks the Shell for a file's icon, the same one Explorer shows. Call on an STA thread.</summary>
internal static class ShellImage
{
    private const int SmallestFullSize = 48;
    private const int FrameProbeInset = 12;
    private const byte FrameAlpha = 20;

    /// <summary>
    /// Returns a frozen <paramref name="size"/>×<paramref name="size"/> icon (a shortcut's target icon, without the arrow
    /// overlay), or <see langword="null"/> if the Shell can't produce one. Never a content thumbnail.
    /// </summary>
    public static BitmapSource? GetIcon(string path, int size)
    {
        try
        {
            PInvoke.SHCreateItemFromParsingName(path, null, out IShellItemImageFactory factory).ThrowOnFailure();
            try
            {
                var icon = GetImage(factory, size);

                // An icon that has no large image comes back as a small one centered in a framed box. Ask for the
                // small size instead, which fills the whole bitmap (the preview scales it up).
                return LooksFramed(icon) ? GetImage(factory, SmallestFullSize) : icon;
            }
            finally
            {
                Marshal.ReleaseComObject(factory);
            }
        }
        catch (Exception exception) when (exception is COMException or IOException or UnauthorizedAccessException)
        {
            Log.Info($"No shell icon for {path}: {exception.Message}");
            return null;
        }
    }

    private static BitmapSource GetImage(IShellItemImageFactory factory, int size)
    {
        factory.GetImage(new SIZE(size, size), SIIGBF.SIIGBF_ICONONLY, out var bitmap);
        using (bitmap)
        {
            return ToBitmapSource(new HBITMAP(bitmap.DangerousGetHandle()));
        }
    }

    // The frame is a faint line at the edge (alpha 38, 77, 51, 26 over the first pixels) with a transparent
    // gap inside it, measured on a 256 px image.
    private static bool LooksFramed(BitmapSource image)
    {
        if (image.PixelWidth < FrameProbeInset * 4)
        {
            return false;
        }

        var middle = image.PixelHeight / 2;
        return AlphaAt(image, 1, middle) > FrameAlpha && AlphaAt(image, FrameProbeInset, middle) == 0
            && AlphaAt(image, middle, 1) > FrameAlpha && AlphaAt(image, middle, FrameProbeInset) == 0;
    }

    private static byte AlphaAt(BitmapSource image, int x, int y)
    {
        var pixel = new byte[4];
        image.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
        return pixel[3];
    }

    // CreateBitmapSourceFromHBitmap drops the alpha channel, so copy the DIB section's pixels ourselves.
    private static unsafe BitmapSource ToBitmapSource(HBITMAP bitmap)
    {
        DIBSECTION dib;
        if (PInvoke.GetObject(bitmap, sizeof(DIBSECTION), &dib) == 0 || dib.dsBm.bmBitsPixel != 32)
        {
            throw new COMException("Shell image is not a 32-bit DIB section.");
        }

        int width = dib.dsBm.bmWidth, height = dib.dsBm.bmHeight, stride = dib.dsBm.bmWidthBytes;
        var bottomUp = dib.dsBmih.biHeight > 0;
        var pixels = new byte[stride * height];
        var source = (byte*)dib.dsBm.bmBits;
        for (var row = 0; row < height; row++)
        {
            var sourceRow = bottomUp ? height - 1 - row : row;
            new ReadOnlySpan<byte>(source + sourceRow * stride, stride).CopyTo(pixels.AsSpan(row * stride));
        }

        // Straight (not premultiplied) alpha: measured, many edge pixels have color above alpha.
        var result = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        result.Freeze();
        return result;
    }
}
