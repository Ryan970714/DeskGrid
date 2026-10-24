using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using DeskFolders.Diagnostics;

namespace DeskFolders.Tests;

internal static class TestSetup
{
    [ModuleInitializer]
    internal static void RedirectLog() =>
        Log.FilePath = Path.Combine(Path.GetTempPath(), "DeskFolders.Tests", "test.log");
}

/// <summary>A unique, auto-deleted directory under %TEMP%.</summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() => Directory.CreateDirectory(Path);

    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "DeskFolders.Tests", Guid.NewGuid().ToString("N"));

    public string Combine(string relativePath) => System.IO.Path.Combine(Path, relativePath);

    /// <summary>Creates an empty file (and its folders) at <paramref name="relativePath"/>.</summary>
    public string CreateFile(string relativePath, FileAttributes attributes = FileAttributes.Normal)
    {
        var path = Combine(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "");
        File.SetAttributes(path, attributes);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A watcher may still hold a handle; %TEMP% gets cleaned eventually.
        }
    }
}

/// <summary>A WPF dispatcher on its own STA thread, standing in for the app's UI thread.</summary>
internal sealed class DispatcherThread : IDisposable
{
    private readonly Dispatcher _dispatcher;

    public DispatcherThread()
    {
        var started = new TaskCompletionSource<Dispatcher>();
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            started.SetResult(Dispatcher.CurrentDispatcher);
            Dispatcher.Run();
        })
        {
            IsBackground = true,
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        _dispatcher = started.Task.GetAwaiter().GetResult();
    }

    public Task<T> InvokeAsync<T>(Func<T> func) => _dispatcher.InvokeAsync(func).Task;

    public Task InvokeAsync(Action action) => _dispatcher.InvokeAsync(action).Task;

    public void Dispose() => _dispatcher.InvokeShutdown();
}
