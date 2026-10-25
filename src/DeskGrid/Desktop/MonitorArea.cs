using System.Drawing;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;

namespace DeskGrid.Desktop;

/// <summary>A monitor's work area (the desktop minus the taskbar) and its current DPI.</summary>
internal readonly record struct MonitorArea(ScreenRect WorkArea, uint Dpi)
{
    /// <summary>The primary monitor always contains the virtual-screen origin.</summary>
    public static MonitorArea Primary => At(new ScreenPoint(0, 0));

    /// <summary>The monitor containing <paramref name="point"/>, or the nearest one.</summary>
    public static unsafe MonitorArea At(ScreenPoint point)
    {
        var monitor = PInvoke.MonitorFromPoint(new Point(point.X, point.Y), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
        PInvoke.GetMonitorInfo(monitor, ref info);
        PInvoke.GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpi, out _).ThrowOnFailure();

        var work = info.rcWork;
        return new MonitorArea(new ScreenRect(work.left, work.top, work.right, work.bottom), dpi);
    }

    /// <summary>Converts device-independent units (1/96 inch) to this monitor's pixels.</summary>
    public int ToPixels(double dip) => (int)Math.Round(dip * Dpi / 96);
}
