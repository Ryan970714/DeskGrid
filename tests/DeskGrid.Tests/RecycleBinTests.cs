using DeskGrid.Desktop;
using DeskGrid.Shell;
using DeskGrid.Threading;

namespace DeskGrid.Tests;

public sealed class RecycleBinTests : IDisposable
{
    private readonly DispatcherThread _sta = new("Test STA");

    public void Dispose() => _sta.Dispose();

    [Fact]
    public async Task DisplayName_IsTheNameExplorerShows_NotTheParsingName()
    {
        var name = await _sta.InvokeAsync(() => RecycleBin.DisplayName);

        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.DoesNotContain("::{", name);
    }
}

public sealed class DesktopIconGridTests
{
    private static DesktopIcon Icon(int index, int left, int top) =>
        new(index, new ScreenRect(left, top, left + 150, top + 180), new System.Drawing.Point(0, 0), new ScreenRect(left, top, left + 150, top + 100));

    private readonly DesktopIconGrid _grid = new(150, 180, new ScreenPoint(0, 0), [Icon(0, 0, 0), Icon(1, 150, 0)], false);

    [Fact]
    public void IconAt_FindsTheIconWhoseCellHoldsThePoint()
    {
        Assert.Equal(0, _grid.IconAt(new ScreenPoint(10, 170))?.Index);
        Assert.Equal(1, _grid.IconAt(new ScreenPoint(150, 0))?.Index); // The left edge belongs to the cell, the right one doesn't.
    }

    [Fact]
    public void IconAt_NoIconThere_IsNull() =>
        Assert.Null(_grid.IconAt(new ScreenPoint(400, 10)));
}

public sealed class ScreenRectTests
{
    [Theory]
    [InlineData(10, 20, true)]
    [InlineData(19, 39, true)]
    [InlineData(20, 20, false)] // Right and bottom edges are outside.
    [InlineData(10, 40, false)]
    [InlineData(9, 20, false)]
    public void Contains_IncludesTheTopLeftEdgesOnly(int x, int y, bool expected) =>
        Assert.Equal(expected, new ScreenRect(10, 20, 20, 40).Contains(new ScreenPoint(x, y)));
}
