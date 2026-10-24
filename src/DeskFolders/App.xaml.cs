using System.Windows;
using System.Windows.Threading;
using DeskFolders.Config;
using DeskFolders.Desktop;
using DeskFolders.Diagnostics;
using DeskFolders.Folders;
using DeskFolders.Icons;
using DeskFolders.Resources;
using DeskFolders.Widgets;

namespace DeskFolders;

public partial class App : Application
{
    private DesktopHost? _desktopHost;
    private IconService? _icons;
    private WidgetBoard? _board;
    private WidgetCatalog? _catalog;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;
        Log.Info("DeskFolders starting.");

        var configStore = ConfigStore.Open(ConfigStore.DefaultFilePath);
        EnsureUsableRootFolder(configStore);

        _desktopHost = new DesktopHost();
        _desktopHost.Start();
        _icons = new IconService();
        _board = new WidgetBoard(_desktopHost, configStore, _icons);
        _board.WidgetClicked += (_, folder) => Log.Info($"Widget \"{folder.Name}\" clicked.");

        _catalog = new WidgetCatalog(configStore);
        _catalog.Changed += (_, _) =>
        {
            _icons.Retain(_catalog.Widgets);
            _board.Show(_catalog.Widgets);
        };
        _catalog.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _catalog?.Dispose();
        _board?.Dispose();
        _icons?.Dispose();
        _desktopHost?.Dispose();
        Log.Info("DeskFolders exited.");
        base.OnExit(e);
    }

    private static void EnsureUsableRootFolder(ConfigStore configStore)
    {
        var config = configStore.Config;
        if (RootFolder.Prepare(config.RootPath) is not { } problem)
        {
            return;
        }

        var fallback = AppConfig.DefaultRootPath;
        if (RootFolder.Prepare(fallback) is { } fallbackProblem)
        {
            throw new InvalidOperationException($"Root folder \"{config.RootPath}\" is unusable ({problem}), and so is the default \"{fallback}\" ({fallbackProblem}).");
        }

        Log.Info($"Root folder \"{config.RootPath}\" rejected ({problem}); falling back to \"{fallback}\".");
        MessageBox.Show(string.Format(Strings.RootFolderRejected, config.RootPath, problem, fallback),
            Strings.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
        config.RootPath = fallback;
        configStore.Save();
    }

    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) =>
        Log.Error("Unhandled exception", e.Exception);
}
