using System.IO;
using System.Windows.Media;
using DeskGrid.Config;
using DeskGrid.Desktop;
using DeskGrid.Diagnostics;
using DeskGrid.Folders;
using DeskGrid.Icons;
using DeskGrid.Panel;
using DeskGrid.Threading;

namespace DeskGrid.Tests;

public sealed class PanelPlacementTests
{
    private const int Gap = 8;
    private static readonly ScreenRect WorkArea = new(0, 0, 1000, 800);

    [Fact]
    public void Place_BelowAndCentered_WhenThereIsRoom() =>
        Assert.Equal(new ScreenPoint(300, 108), PanelPlacement.Place(new ScreenRect(400, 0, 500, 100), 300, 200, WorkArea, Gap));

    [Fact]
    public void Place_ShiftsSidewaysToStayOnScreen() =>
        Assert.Equal(new ScreenPoint(700, 108), PanelPlacement.Place(new ScreenRect(900, 0, 1000, 100), 300, 200, WorkArea, Gap));

    [Fact]
    public void Place_Above_WhenBelowDoesNotFit() =>
        Assert.Equal(new ScreenPoint(300, 492), PanelPlacement.Place(new ScreenRect(400, 700, 500, 800), 300, 200, WorkArea, Gap));

    [Fact]
    public void Place_Left_WhenNeitherBelowNorAboveFits() =>
        Assert.Equal(new ScreenPoint(592, 100), PanelPlacement.Place(new ScreenRect(900, 300, 1000, 400), 300, 500, WorkArea, Gap));

    [Fact]
    public void Place_Right_WhenOnlyRightFits() =>
        Assert.Equal(new ScreenPoint(108, 100), PanelPlacement.Place(new ScreenRect(0, 300, 100, 400), 300, 500, WorkArea, Gap));

    [Fact]
    public void Place_TooBigForAnySide_ClampsIntoTheWorkArea() =>
        Assert.Equal(new ScreenPoint(0, 0), PanelPlacement.Place(new ScreenRect(400, 300, 500, 400), 2000, 2000, WorkArea, Gap));
}

public sealed class PanelThemeTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void System_UsesAcrylicWhereSupported_AndFollowsTheSystemTheme(bool dark, bool acrylicSupported)
    {
        var theme = PanelTheme.Resolve(new PanelBackground(), dark, acrylicSupported);

        Assert.Equal(acrylicSupported, theme.UseAcrylic);
        Assert.Equal(dark, theme.IsDark);
        Assert.Equal(255, theme.Background.A);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Presets_AreTranslucentAndIgnoreTheSystemTheme(bool dark)
    {
        var style = dark ? PanelBackgroundStyle.Dark : PanelBackgroundStyle.Light;
        var theme = PanelTheme.Resolve(new PanelBackground { Style = style }, systemDark: !dark, acrylicSupported: true);

        Assert.False(theme.UseAcrylic);
        Assert.Equal(dark, theme.IsDark);
        Assert.InRange(theme.Background.A, 1, 254);
    }

    [Fact]
    public void Custom_UsesColorAndOpacity_WithReadableText()
    {
        var bright = PanelTheme.Resolve(Custom("#F4C430", 0.5), systemDark: true, acrylicSupported: true);
        var dim = PanelTheme.Resolve(Custom("#1E3A5F", 2), systemDark: false, acrylicSupported: true);

        Assert.Equal(Color.FromArgb(128, 0xF4, 0xC4, 0x30), bright.Background);
        Assert.False(bright.IsDark);
        Assert.Equal(255, dim.Background.A);
        Assert.True(dim.IsDark);
    }

    [Theory]
    [InlineData("not a color")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#12")]
    [InlineData("#GGGGGG")]
    public void Custom_InvalidColor_FallsBackToTheDefault(string color)
    {
        var theme = PanelTheme.Resolve(Custom(color, 1), systemDark: false, acrylicSupported: false);

        Assert.Equal(ColorConverter.ConvertFromString(new PanelBackground().CustomColor), theme.Background);
    }

    private static PanelBackground Custom(string color, double opacity) =>
        new() { Style = PanelBackgroundStyle.Custom, CustomColor = color, CustomOpacity = opacity };
}

public sealed class PopupPanelTests : IDisposable
{
    private readonly DispatcherThread _ui = new("Test UI");

    public void Dispose() => _ui.Dispose();

    [Fact]
    public async Task ShowFolder_AfterTheList_GrowsToTheFolderItems()
    {
        var (listHeight, folderHeight) = await _ui.InvokeAsync(() =>
        {
            using var icons = new IconService();
            var theme = PanelTheme.Resolve(new PanelBackground { Style = PanelBackgroundStyle.Dark }, false, false);
            var panel = new PopupPanel(new ScreenRect(0, 0, 1, 1), 3, theme, icons);
            var big = Folder("Big", 17);
            panel.ShowList([Folder("A", 1), Folder("B", 1), big]);
            var listHeight = panel.Height;
            panel.ShowFolder(big);
            var folderHeight = panel.Height;
            panel.Close();
            return (listHeight, folderHeight);
        });

        // One row of folders, then the most rows a panel shows (4) of the 17 items.
        Assert.Equal(3 * 96, folderHeight - listHeight, precision: 0);
    }

    private static WidgetFolder Folder(string name, int itemCount) =>
        new(name, @"C:\DeskGrid.Tests\" + name,
            [.. Enumerable.Range(1, itemCount).Select(i => new FolderItem($"Item {i}.txt", $@"C:\DeskGrid.Tests\{name}\Item {i}.txt", DateTime.UnixEpoch))]);
}

public sealed class DragGhostTests : IDisposable
{
    private readonly DispatcherThread _ui = new("Test UI");

    public void Dispose() => _ui.Dispose();

    [Fact]
    public async Task ShowAndLand_DrawTheIcon()
    {
        var before = FailureCount();
        var landed = new TaskCompletionSource();
        await _ui.InvokeAsync(() =>
        {
            var icon = new DrawingImage(new GeometryDrawing(Brushes.SteelBlue, null, new RectangleGeometry(new(0, 0, 48, 48))));
            var ghost = new DragGhost(icon, new ScreenPoint(200, 200));
            ghost.Show();
            ghost.Lift();
            ghost.Land(new ScreenPoint(260, 260), vanish: false, landed.SetResult);
        });
        await landed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // A window WPF also renders into refuses UpdateLayeredWindow, and the ghost shows as a black square.
        Assert.Equal(before, FailureCount());
    }

    private static int FailureCount() =>
        File.Exists(Log.FilePath) ? File.ReadLines(Log.FilePath).Count(line => line.Contains("Drawing the drag ghost failed")) : 0;
}
