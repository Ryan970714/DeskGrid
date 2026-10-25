namespace DeskGrid.Desktop;

/// <summary>A point in physical pixels, in virtual-screen coordinates.</summary>
internal readonly record struct ScreenPoint(int X, int Y)
{
    public ScreenPoint Offset(int dx, int dy) => new(X + dx, Y + dy);
}

/// <summary>A rectangle in physical pixels, in virtual-screen coordinates.</summary>
internal readonly record struct ScreenRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;
}
