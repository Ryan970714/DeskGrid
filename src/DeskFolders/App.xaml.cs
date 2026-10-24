using System.Windows;
using System.Windows.Threading;
using DeskFolders.Desktop;
using DeskFolders.Diagnostics;
using DeskFolders.Spike;

namespace DeskFolders;

public partial class App : Application
{
    private DesktopHost? _desktopHost;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;
        Log.Info("DeskFolders starting.");

        _desktopHost = new DesktopHost();
        _desktopHost.Add(new DesktopSurface(new SpikeTile(), SpikeTile.CenterOfPrimaryScreen()));
        _desktopHost.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _desktopHost?.Dispose();
        Log.Info("DeskFolders exited.");
        base.OnExit(e);
    }

    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) =>
        Log.Error("Unhandled exception", e.Exception);
}
