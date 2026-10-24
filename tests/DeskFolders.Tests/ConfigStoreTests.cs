using System.IO;
using DeskFolders.Config;
using DeskFolders.Desktop;

namespace DeskFolders.Tests;

public sealed class ConfigStoreTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    private string ConfigPath => _temp.Combine("config.json");

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Open_MissingFile_ReturnsDefaults()
    {
        var config = ConfigStore.Open(ConfigPath).Config;

        Assert.Equal(AppConfig.DefaultRootPath, config.RootPath);
        Assert.Equal(AppConfig.DefaultPanelColumns, config.PanelColumns);
        Assert.Equal(DropMode.Move, config.DropMode);
        Assert.Equal(PanelBackgroundStyle.System, config.PanelBackground.Style);
        Assert.Empty(config.Widgets);
    }

    [Fact]
    public void Save_ThenOpen_RoundTrips()
    {
        var store = ConfigStore.Open(ConfigPath);
        store.Config.RootPath = @"D:\Folders";
        store.Config.PanelColumns = 4;
        store.Config.DropMode = DropMode.Copy;
        store.Config.PanelBackground = new PanelBackground { Style = PanelBackgroundStyle.Custom, CustomColor = "#112233", CustomOpacity = 0.5 };
        store.Config.Widgets["Games"] = new WidgetConfig { Position = new ScreenPoint(10, 20), ItemOrder = ["b.lnk", "a.lnk"] };
        store.Save();

        var reloaded = ConfigStore.Open(ConfigPath).Config;

        Assert.Equal(@"D:\Folders", reloaded.RootPath);
        Assert.Equal(4, reloaded.PanelColumns);
        Assert.Equal(DropMode.Copy, reloaded.DropMode);
        Assert.Equal((PanelBackgroundStyle.Custom, "#112233", 0.5),
            (reloaded.PanelBackground.Style, reloaded.PanelBackground.CustomColor, reloaded.PanelBackground.CustomOpacity));
        Assert.Equal(new ScreenPoint(10, 20), reloaded.Widgets["GAMES"].Position);
        Assert.Equal(["b.lnk", "a.lnk"], reloaded.Widgets["Games"].ItemOrder);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{ "rootPath": null }""")]
    public void Open_InvalidFile_MovesItAsideAndReturnsDefaults(string content)
    {
        File.WriteAllText(ConfigPath, content);

        var config = ConfigStore.Open(ConfigPath).Config;

        Assert.Equal(AppConfig.DefaultRootPath, config.RootPath);
        Assert.False(File.Exists(ConfigPath));
        var backup = Assert.Single(Directory.GetFiles(_temp.Path, "config.json.*.bad"));
        Assert.Equal(content, File.ReadAllText(backup));
    }
}
