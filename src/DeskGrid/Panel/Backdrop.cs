using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.Controls;

namespace DeskGrid.Panel;

/// <summary>The system acrylic backdrop that Windows 11 draws behind transient windows (flyouts, menus).</summary>
internal static class Backdrop
{
    /// <summary>DWMWA_SYSTEMBACKDROP_TYPE exists from Windows 11 22H2 (build 22621).</summary>
    public static bool IsAcrylicSupported => Environment.OSVersion.Version.Build >= 22621;

    /// <summary>
    /// Asks DWM for acrylic with rounded corners behind the whole window, tinted for the light or dark
    /// theme. The window must be non-layered, with a transparent composition background.
    /// </summary>
    public static unsafe bool TryApplyAcrylic(HWND window, bool dark)
    {
        var wholeWindow = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        if (PInvoke.DwmExtendFrameIntoClientArea(window, in wholeWindow).Failed)
        {
            return false;
        }

        int darkMode = dark ? 1 : 0;
        var corners = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
        var backdrop = (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW;
        // No fade or zoom when the window appears or goes away: with a backdrop that animation reads as a stutter.
        int noTransitions = 1;
        PInvoke.DwmSetWindowAttribute(window, DWMWINDOWATTRIBUTE.DWMWA_TRANSITIONS_FORCEDISABLED, &noTransitions, sizeof(int));
        PInvoke.DwmSetWindowAttribute(window, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &darkMode, sizeof(int));
        PInvoke.DwmSetWindowAttribute(window, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, &corners, sizeof(int));
        return PInvoke.DwmSetWindowAttribute(window, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, &backdrop, sizeof(int)).Succeeded;
    }

    /// <summary>Removes the backdrop (Mica / acrylic) that a themed window got, leaving the window opaque.</summary>
    public static unsafe void DisableBackdrop(HWND window)
    {
        var none = (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE;
        var noMargins = new MARGINS();
        PInvoke.DwmExtendFrameIntoClientArea(window, in noMargins);
        PInvoke.DwmSetWindowAttribute(window, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, &none, sizeof(int));
    }

    /// <summary>
    /// Hides or shows the window without the Shell knowing, so it can be shown the instant its content and
    /// backdrop are ready instead of appearing half drawn.
    /// </summary>
    public static unsafe void SetCloaked(HWND window, bool cloaked)
    {
        int value = cloaked ? 1 : 0;
        PInvoke.DwmSetWindowAttribute(window, DWMWINDOWATTRIBUTE.DWMWA_CLOAK, &value, sizeof(int));
    }
}
