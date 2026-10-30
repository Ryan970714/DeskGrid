using Windows.Win32;

namespace DeskGrid.Desktop;

/// <summary>A point in physical pixels, in virtual-screen coordinates.</summary>
internal readonly record struct ScreenPoint(int X, int Y)
{
    /// <summary>Where the mouse pointer is now.</summary>
    public static ScreenPoint AtCursor()
    {
        PInvoke.GetCursorPos(out var cursor);
        return new ScreenPoint(cursor.X, cursor.Y);
    }

    public ScreenPoint Offset(int dx, int dy) => new(X + dx, Y + dy);
}

/// <summary>A rectangle in physical pixels, in virtual-screen coordinates.</summary>
internal readonly record struct ScreenRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;

    public bool Contains(ScreenPoint point) =>
        point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;
}
