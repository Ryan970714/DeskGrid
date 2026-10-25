using System.IO;
using System.IO.Pipes;
using DeskGrid.Diagnostics;
using Windows.Win32;

namespace DeskGrid.Taskbar;

/// <summary>
/// How a second DeskGrid process (started from a taskbar shortcut) reaches the running one: it writes the
/// folder name to a per-user named pipe and exits.
/// </summary>
internal sealed class InstanceChannel : IDisposable
{
    // Grants the running instance the right to take the foreground, which this process has just been given.
    private const uint AnyProcess = 0xFFFFFFFF;

    private static readonly string PipeName = $"DeskGrid.Open.{Environment.UserName}";

    private readonly CancellationTokenSource _stop = new();

    /// <summary>Listens for requests. <paramref name="onRequest"/> runs on a thread-pool thread.</summary>
    public InstanceChannel(Action<string> onRequest) => _ = ListenAsync(onRequest, _stop.Token);

    public static void Send(string folderName)
    {
        try
        {
            PInvoke.AllowSetForegroundWindow(AnyProcess);
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(2000);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine(folderName);
        }
        catch (Exception exception) when (exception is IOException or TimeoutException or UnauthorizedAccessException)
        {
            Log.Error("Passing the request to the running DeskGrid failed", exception);
        }
    }

    public void Dispose() => _stop.Cancel();

    private static async Task ListenAsync(Action<string> onRequest, CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(stop);
                using var reader = new StreamReader(server);
                if (await reader.ReadLineAsync(stop) is { Length: > 0 } name)
                {
                    onRequest(name);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A client that vanished mid-message, or a name clash: log, then keep listening.
                Log.Error("Reading a request from another DeskGrid failed", exception);
                try
                {
                    await Task.Delay(1000, stop);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
