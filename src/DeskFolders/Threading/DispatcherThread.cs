using System.Windows.Threading;

namespace DeskFolders.Threading;

/// <summary>
/// A WPF dispatcher on its own STA thread: for work that needs an STA (Shell, WPF imaging) but must
/// not block the UI thread.
/// </summary>
internal sealed class DispatcherThread : IDisposable
{
    private readonly Dispatcher _dispatcher;

    public DispatcherThread(string name)
    {
        var started = new TaskCompletionSource<Dispatcher>();
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            started.SetResult(Dispatcher.CurrentDispatcher);
            Dispatcher.Run();
        })
        {
            Name = name,
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
