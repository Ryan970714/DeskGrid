using DeskGrid.Desktop;

namespace DeskGrid.Panel;

/// <summary>Where a panel opens relative to the widget it belongs to.</summary>
internal static class PanelPlacement
{
    /// <summary>
    /// Centered below <paramref name="anchor"/> if that fits the work area, else above, else to the left,
    /// else to the right; then shifted along the other axis to stay inside the work area.
    /// </summary>
    public static ScreenPoint Place(ScreenRect anchor, int width, int height, ScreenRect workArea, int gap)
    {
        var centeredX = (anchor.Left + anchor.Right - width) / 2;
        var centeredY = (anchor.Top + anchor.Bottom - height) / 2;
        (ScreenPoint Position, bool Fits)[] sides =
        [
            (new(centeredX, anchor.Bottom + gap), anchor.Bottom + gap + height <= workArea.Bottom),
            (new(centeredX, anchor.Top - gap - height), anchor.Top - gap - height >= workArea.Top),
            (new(anchor.Left - gap - width, centeredY), anchor.Left - gap - width >= workArea.Left),
            (new(anchor.Right + gap, centeredY), anchor.Right + gap + width <= workArea.Right),
        ];

        var chosen = sides.FirstOrDefault(s => s.Fits, sides[0]).Position;
        return new ScreenPoint(
            Math.Clamp(chosen.X, workArea.Left, Math.Max(workArea.Left, workArea.Right - width)),
            Math.Clamp(chosen.Y, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - height)));
    }
}
