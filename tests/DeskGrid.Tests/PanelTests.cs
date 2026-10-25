using System.Windows.Media;
using DeskGrid.Config;
using DeskGrid.Desktop;
using DeskGrid.Panel;

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

    [Fact]
    public void Custom_InvalidColor_FallsBackToTheDefault()
    {
        var theme = PanelTheme.Resolve(Custom("not a color", 1), systemDark: false, acrylicSupported: false);

        Assert.Equal(ColorConverter.ConvertFromString(new PanelBackground().CustomColor), theme.Background);
    }

    private static PanelBackground Custom(string color, double opacity) =>
        new() { Style = PanelBackgroundStyle.Custom, CustomColor = color, CustomOpacity = opacity };
}
