using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DeskFolders.Desktop;

/// <summary>Desktop window hierarchies produced by Explorer.</summary>
internal enum DesktopLayout
{
    /// <summary>Windows 10 to Windows 11 23H2: the icon view lives in a top-level WorkerW (or Progman).</summary>
    Legacy,

    /// <summary>Windows 11 24H2+: Progman is composition-based and hosts the icon view itself.</summary>
    Modern,
}

/// <summary>
/// Explorer's <c>SHELLDLL_DefView</c> window, parent of the desktop icon list view. Surfaces attach
/// here, above the icons, so they receive input yet stay behind every normal window.
/// </summary>
internal readonly record struct IconLayer(HWND Handle, DesktopLayout Layout)
{
    // Undocumented Progman message that splits the wallpaper into its own WorkerW (wallpaper engines use it).
    private const uint SpawnWorkerWMessage = 0x052C;

    /// <summary>Locates the icon layer, or returns <see langword="null"/> while Explorer is not (fully) running.</summary>
    public static IconLayer? Find()
    {
        var progman = FindChild(HWND.Null, "Progman");
        if (progman.IsNull)
        {
            return null;
        }

        // Settle the hierarchy up front: once split, it no longer changes when other apps send the
        // same message. A no-op if already split.
        PInvoke.SendMessageTimeout(progman, SpawnWorkerWMessage, 0xD, 0x1, SEND_MESSAGE_TIMEOUT_FLAGS.SMTO_NORMAL, 1000, out _);

        // Modern layout (and legacy before the split): Progman > SHELLDLL_DefView.
        var defView = FindChild(progman, "SHELLDLL_DefView");
        if (!defView.IsNull)
        {
            return new IconLayer(defView, IsCompositionBased(progman) ? DesktopLayout.Modern : DesktopLayout.Legacy);
        }

        // Legacy layout after the split: top-level WorkerW > SHELLDLL_DefView.
        for (var worker = FindChild(HWND.Null, "WorkerW"); !worker.IsNull; worker = FindChild(HWND.Null, "WorkerW", worker))
        {
            defView = FindChild(worker, "SHELLDLL_DefView");
            if (!defView.IsNull)
            {
                return new IconLayer(defView, DesktopLayout.Legacy);
            }
        }

        return null;
    }

    private static HWND FindChild(HWND parent, string className, HWND after = default) =>
        PInvoke.FindWindowEx(parent, after, className, null);

    private static bool IsCompositionBased(HWND progman) =>
        ((WINDOW_EX_STYLE)PInvoke.GetWindowLong(progman, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE))
            .HasFlag(WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP);
}
