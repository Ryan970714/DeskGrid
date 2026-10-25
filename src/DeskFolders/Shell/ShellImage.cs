using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeskFolders.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;

namespace DeskFolders.Shell;

/// <summary>Asks the Shell for a file's icon, the same one Explorer shows. Call on an STA thread.</summary>
internal static class ShellImage
{
    /// <summary>
    /// Returns a frozen <paramref name="size"/>Â² icon (a shortcut's target icon, without the arrow
    /// overlay), or <see langword="null"/> if the Shell can't produce one. Never a content thumbnail.
    /// </summary>
    public static BitmapSource? GetIcon(string path, int size)
    {
        try
        {
            PInvoke.SHCreateItemFromParsingName(path, null, out IShellItemImageFactory factory).ThrowOnFailure();
            try
            {
                factory.GetImage(new SIZE(size, size), SIIGBF.SIIGBF_ICONONLY, out var bitmap);
                using (bitmap)
                {
                    return ToBitmapSource(new HBITMAP(bitmap.DangerousGetHandle()));
                }
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
