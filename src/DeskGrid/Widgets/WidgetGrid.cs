using DeskGrid.Desktop;

namespace DeskGrid.Widgets;

/// <summary>
/// The cells widgets snap to: widget-sized, laid out from the work area's top-right corner (away from
/// the icons Windows arranges from the top-left) and filled down each column, right to left.
/// A cell is identified by its top-left corner.
/// </summary>
internal sealed class WidgetGrid(ScreenRect workArea, int cellWidth, int cellHeight)
{
    private int Columns => Math.Max(1, workArea.Width / cellWidth);

    private int Rows => Math.Max(1, workArea.Height / cellHeight);

    /// <summary>The first cell in fill order that isn't <paramref name="taken"/>.</summary>
    public ScreenPoint FirstFree(IReadOnlySet<ScreenPoint> taken) =>
        Cells().Where(c => !taken.Contains(c)).DefaultIfEmpty(CellAt(0, 0)).First();

    /// <summary>The cell closest to <paramref name="position"/> that isn't <paramref name="taken"/>.</summary>
    public ScreenPoint NearestFree(ScreenPoint position, IReadOnlySet<ScreenPoint> taken)
    {
        var free = Cells().Where(c => !taken.Contains(c)).ToList();
        return (free.Count > 0 ? free : Cells()).MinBy(c => DistanceSquared(c, position));
    }

    private IEnumerable<ScreenPoint> Cells()
    {
        for (var column = 0; column < Columns; column++)
        {
            for (var row = 0; row < Rows; row++)
            {
                yield return CellAt(column, row);
            }
        }
    }

    // Columns count from the right edge.
    private ScreenPoint CellAt(int column, int row) =>
        new(workArea.Right - (column + 1) * cellWidth, workArea.Top + row * cellHeight);

    private static long DistanceSquared(ScreenPoint a, ScreenPoint b)
    {
        long dx = a.X - b.X, dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
