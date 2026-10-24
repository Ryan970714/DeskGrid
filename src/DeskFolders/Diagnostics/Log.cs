using System.Diagnostics;
using System.IO;

namespace DeskFolders.Diagnostics;

/// <summary>Minimal append-only log at <c>%LOCALAPPDATA%\DeskFolders\deskfolders.log</c>.</summary>
internal static class Log
{
    private static readonly Lock Gate = new();

    // Settable so tests don't write into the real log.
    public static string FilePath { get; internal set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeskFolders", "deskfolders.log");

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception exception) => Write("ERROR", $"{message}: {exception}");

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        Debug.WriteLine(line);

        // Logging must never take the app down.
        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.AppendAllText(FilePath, line + Environment.NewLine);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
