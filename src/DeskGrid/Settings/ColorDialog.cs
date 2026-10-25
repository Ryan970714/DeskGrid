using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls.Dialogs;

namespace DeskGrid.Settings;

/// <summary>The Windows color picker (comdlg32), since WPF has none.</summary>
internal static class ColorDialog
{
    // The 16 "custom colors" slots, kept for the session as Windows' own apps do.
    private static readonly COLORREF[] CustomColors = new COLORREF[16];

    /// <summary>Returns the picked color, or <see langword="null"/> if the user cancelled.</summary>
    public static unsafe Color? Pick(Window owner, Color initial)
    {
        fixed (COLORREF* customColors = CustomColors)
        {
            var options = new CHOOSECOLORW
            {
                lStructSize = (uint)Marshal.SizeOf<CHOOSECOLORW>(),
                hwndOwner = new HWND(new WindowInteropHelper(owner).Handle),
                rgbResult = new COLORREF((uint)(initial.R | initial.G << 8 | initial.B << 16)),
                lpCustColors = customColors,
                Flags = CHOOSECOLOR_FLAGS.CC_RGBINIT | CHOOSECOLOR_FLAGS.CC_FULLOPEN,
            };
            if (!PInvoke.ChooseColor(ref options))
            {
                return null;
            }

            var rgb = options.rgbResult.Value;
            return Color.FromRgb((byte)rgb, (byte)(rgb >> 8), (byte)(rgb >> 16));
        }
    }
}
