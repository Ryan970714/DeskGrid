using System.IO;
using System.Windows;
using System.Windows.Threading;
using DeskGrid.Config;
using DeskGrid.Desktop;
using DeskGrid.Diagnostics;
using DeskGrid.Folders;
using DeskGrid.Icons;
using DeskGrid.Panel;
using DeskGrid.Resources;
using DeskGrid.Settings;
using DeskGrid.Shell;
using DeskGrid.Tray;
using DeskGrid.Widgets;

namespace DeskGrid;

public partial class App : Application
{
    // Per sign-in session: a second launch (e.g. auto-start plus a manual start) just exits.
    private const string SingleInstanceName = @"Local\DeskGrid.SingleInstance";

    private Mutex? _singleInstance;
    private ConfigStore? _configStore;
    private AutoStart? _autoStart;
    private DesktopHost? _desktopHost;
    private IconService? _icons;
    private PanelController? _panels;
    private TrayIcon? _tray;
    private WidgetBoard? _board;
    private WidgetCatalog? _catalog;
    private SettingsWindow? _settings;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _singleInstance = new Mutex(initiallyOwned: true, SingleInstanceName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            Log.Info("DeskGrid is already running; this instance exits.");
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnUnhandledException;
        Log.Info("DeskGrid starting.");

        _configStore = ConfigStore.Open(ConfigStore.DefaultFilePath);
        EnsureUsableRootFolder(_configStore);
        _autoStart = AutoStart.ForCurrentUser();
        _autoStart.UpdatePath();

        _desktopHost = new DesktopHost();
        _desktopHost.Start();
        _icons = new IconService();
        _panels = new PanelController(_configStore, _icons);
        _tray = new TrayIcon(Strings.AppName, TrayMenu);
        StartWidgets();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _settings?.Close();
        StopWidgets();
        _tray?.Dispose();
        _icons?.Dispose();
        _desktopHost?.Dispose();
        _singleInstance?.Dispose();
        Log.Info("DeskGrid exited.");
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

    private IReadOnlyList<TrayMenuItem> TrayMenu() =>
    [
        new(Strings.TrayNewFolder, CreateFolder),
        new(Strings.TrayOpenRoot, () => ShellItem.Open(_configStore!.Config.RootPath)),
        new(Strings.TraySettings, ShowSettings),
        TrayMenuItem.Separator,
        new(Strings.TrayAutoStart, () => _autoStart!.SetEnabled(!_autoStart.IsEnabled), _autoStart!.IsEnabled),
        TrayMenuItem.Separator,
        new(Strings.MenuExit, Shutdown),
    ];

    // The widgets of the current root folder; rebuilt when the root changes.
    private void StartWidgets()
    {
        var board = new WidgetBoard(_desktopHost!, _configStore!, _icons!);
        board.WidgetClicked += (_, click) => _panels!.Toggle(click.Folder, click.Bounds);

        var catalog = new WidgetCatalog(_configStore!);
        catalog.Changed += (_, _) =>
        {
            _icons!.Retain(catalog.Widgets);
            board.Show(catalog.Widgets);
            _panels!.Refresh(catalog.Widgets);
        };

        (_board, _catalog) = (board, catalog);
        catalog.Start();
    }

    private void StopWidgets()
    {
        _panels?.Close();
        _catalog?.Dispose();
        _board?.Dispose();
        (_board, _catalog) = (null, null);
    }

    private void CreateFolder()
    {
        var rootPath = _configStore!.Config.RootPath;
        var dialog = new NewFolderDialog(rootPath);
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            // The watcher turns it into a widget.
            Directory.CreateDirectory(Path.Combine(rootPath, dialog.FolderName));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log.Error("Creating a widget folder failed", exception);
            MessageBox.Show(exception.Message, Strings.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowSettings()
    {
        if (_settings is { } open)
        {
            open.Activate();
            return;
        }

        var settings = new SettingsWindow(_configStore!.Config);
        settings.Applied += (_, choice) => ApplySettings(choice);
        settings.Closed += (_, _) => _settings = null;
        _settings = settings;
        settings.Show();
        settings.Activate();
    }

    private void ApplySettings(SettingsChoice choice)
    {
        var config = _configStore!.Config;
        var rootChanged = !string.Equals(Path.GetFullPath(choice.RootPath), Path.GetFullPath(config.RootPath), StringComparison.OrdinalIgnoreCase);
        if (rootChanged)
        {
            StopWidgets();
        }

        config.RootPath = choice.RootPath;
        config.PanelColumns = choice.PanelColumns;
        config.DropMode = choice.DropMode;
        config.PanelBackground = choice.PanelBackground;
        _configStore.Save();

        if (rootChanged)
        {
            StartWidgets();
        }
        else
        {
            // Panels read their look when they open.
            _panels!.Close();
        }
    }
}
