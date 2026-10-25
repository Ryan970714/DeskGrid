using System.IO;
using DeskGrid.Config;
using DeskGrid.Desktop;
using DeskGrid.Folders;
using DeskGrid.Threading;

namespace DeskGrid.Tests;

public sealed class WidgetCatalogTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly TempDirectory _temp = new();
    private readonly DispatcherThread _ui = new("Test UI");

    private string RootPath => _temp.Combine("root");

    private string ConfigPath => _temp.Combine("config.json");

    public void Dispose()
    {
        _ui.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public void Reconcile_NewFolder_GetsSettingsWithItemsInScanOrder()
    {
        var settings = Settings();

        var changed = WidgetCatalog.Reconcile(settings, [Widget("Games", "a", "b")], out var ordered);

        Assert.True(changed);
        Assert.Equal(["a", "b"], settings["Games"].ItemOrder);
        Assert.Null(settings["Games"].Position);
        Assert.Equal(["a", "b"], ItemNames(ordered[0]));
    }

    [Fact]
    public void Reconcile_AppliesSavedOrder_AppendsNewItems_DropsMissingOnes()
    {
        var settings = Settings(("Games", ["c", "gone", "a"]));

        var changed = WidgetCatalog.Reconcile(settings, [Widget("Games", "a", "b", "c")], out var ordered);

        Assert.True(changed);
        Assert.Equal(["c", "a", "b"], ItemNames(ordered[0]));
        Assert.Equal(["c", "a", "b"], settings["Games"].ItemOrder);
    }

    [Fact]
    public void Reconcile_DropsSettingsOfMissingFolders()
    {
        var settings = Settings(("Games", ["a"]), ("Gone", ["x"]));

        var changed = WidgetCatalog.Reconcile(settings, [Widget("Games", "a")], out _);

        Assert.True(changed);
        Assert.Equal(["Games"], settings.Keys);
    }

    [Fact]
    public void Reconcile_ReturnsFalseWhenNothingChanged()
    {
        var settings = Settings(("Games", ["b", "a"]));

        Assert.False(WidgetCatalog.Reconcile(settings, [Widget("GAMES", "a", "b")], out var ordered));
        Assert.Equal(["b", "a"], ItemNames(ordered[0]));
    }

    [Fact]
    public void Reconcile_RemovesDuplicateNames()
    {
        var settings = Settings(("Games", ["a", "A", "b"]));

        WidgetCatalog.Reconcile(settings, [Widget("Games", "a", "b")], out _);

        Assert.Equal(["a", "b"], settings["Games"].ItemOrder);
    }

    [Fact]
    public async Task RenamingWidgetFolder_MovesItsSettings()
    {
        _temp.CreateFile(@"root\Games\a.lnk");
        _temp.CreateFile(@"root\Games\b.lnk");
        var catalog = await StartCatalogAsync(config =>
            config.Widgets["Games"] = new WidgetConfig { Position = new ScreenPoint(5, 6), ItemOrder = ["b.lnk", "a.lnk"] });

        Directory.Move(Path.Combine(RootPath, "Games"), Path.Combine(RootPath, "Play"));
        await WaitUntilAsync(catalog, () => catalog.Widgets.Any(w => w.Name == "Play"));

        var saved = ConfigStore.Open(ConfigPath).Config;
        Assert.False(saved.Widgets.ContainsKey("Games"));
        Assert.Equal(new ScreenPoint(5, 6), saved.Widgets["Play"].Position);
        Assert.Equal(["b.lnk", "a.lnk"], saved.Widgets["Play"].ItemOrder);
    }

    [Fact]
    public async Task RenamingItem_KeepsItsPlaceInTheOrder()
    {
        _temp.CreateFile(@"root\Games\a.lnk");
        _temp.CreateFile(@"root\Games\b.lnk");
        _temp.CreateFile(@"root\Games\c.lnk");
        var catalog = await StartCatalogAsync(config =>
            config.Widgets["Games"] = new WidgetConfig { ItemOrder = ["b.lnk", "a.lnk", "c.lnk"] });

        File.Move(Path.Combine(RootPath, @"Games\a.lnk"), Path.Combine(RootPath, @"Games\z.lnk"));
        await WaitUntilAsync(catalog, () => catalog.Widgets[0].Items.Any(i => i.Name == "z.lnk"));

        Assert.Equal(["b.lnk", "z.lnk", "c.lnk"], ConfigStore.Open(ConfigPath).Config.Widgets["Games"].ItemOrder);
    }

    [Fact]
    public async Task AddingAndDeletingFolders_UpdatesWidgetsAndSettings()
    {
        _temp.CreateFile(@"root\Old\a.lnk");
        var catalog = await StartCatalogAsync();

        _temp.CreateFile(@"root\New\x.url");
        Directory.Delete(Path.Combine(RootPath, "Old"), recursive: true);
        await WaitUntilAsync(catalog, () => catalog.Widgets.Select(w => w.Name).SequenceEqual(["New"]));

        var saved = ConfigStore.Open(ConfigPath).Config;
        Assert.Equal(["New"], saved.Widgets.Keys);
        Assert.Equal(["x.url"], saved.Widgets["New"].ItemOrder);
    }

    [Fact]
    public async Task BurstOfChanges_IsFoldedIntoFewRefreshes()
    {
        Directory.CreateDirectory(Path.Combine(RootPath, "Games"));
        var catalog = await StartCatalogAsync();
        var refreshes = 0;
        await _ui.InvokeAsync(() => catalog.Changed += (_, _) => refreshes++);

        for (var i = 0; i < 10; i++)
        {
            _temp.CreateFile($@"root\Games\{i}.lnk");
        }

        await WaitUntilAsync(catalog, () => catalog.Widgets[0].Items.Count == 10);
        Assert.InRange(await _ui.InvokeAsync(() => refreshes), 1, 2);
    }

    private static Dictionary<string, WidgetConfig> Settings(params (string Name, List<string> Order)[] widgets) =>
        widgets.ToDictionary(w => w.Name, w => new WidgetConfig { ItemOrder = w.Order }, StringComparer.OrdinalIgnoreCase);

    private static WidgetFolder Widget(string name, params string[] items) =>
        new(name, $@"C:\root\{name}", [.. items.Select(i => new FolderItem(i, $@"C:\root\{name}\{i}", default))]);

    private static IEnumerable<string> ItemNames(WidgetFolder widget) => widget.Items.Select(i => i.Name);

    private async Task<WidgetCatalog> StartCatalogAsync(Action<AppConfig>? configure = null)
    {
        var store = ConfigStore.Open(ConfigPath);
        store.Config.RootPath = RootPath;
        configure?.Invoke(store.Config);

        return await _ui.InvokeAsync(() =>
        {
            var catalog = new WidgetCatalog(store);
            catalog.Start();
            return catalog;
        });
    }

    // Completes once condition holds after a refresh; condition runs on the UI thread.
    private async Task WaitUntilAsync(WidgetCatalog catalog, Func<bool> condition)
    {
        var satisfied = await _ui.InvokeAsync(() =>
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void Check(object? sender, EventArgs e)
            {
                if (condition())
                {
                    catalog.Changed -= Check;
                    completion.TrySetResult();
                }
            }

            catalog.Changed += Check;
            Check(null, EventArgs.Empty);
            return completion.Task;
        });

        await satisfied.WaitAsync(Timeout, TestContext.Current.CancellationToken);
    }
}
