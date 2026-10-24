using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using DeskFolders.Diagnostics;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace DeskFolders.Panel;

/// <summary>Shell operations on a folder item. Call on an STA thread (the UI thread is one).</summary>
internal static class ShellItem
{
    /// <summary>
    /// Opens the item exactly like a double-click in Explorer: ShellExecuteEx with the item's folder as the
    /// working directory (a shortcut's own working directory still wins). Windows shows its own error
    /// dialog if the item can't be opened, e.g. a shortcut whose target is gone.
    /// </summary>
    public static void Open(string path)
    {
        var startInfo = new ProcessStartInfo(path)
        {
            UseShellExecute = true,
            ErrorDialog = true,
            WorkingDirectory = Path.GetDirectoryName(path),
        };

        try
        {
            using var process = Process.Start(startInfo);
        }
        catch (Exception exception) when (exception is Win32Exception or FileNotFoundException)
        {
            // Also thrown when the user cancels a UAC prompt.
            Log.Info($"Opening {path} failed: {exception.Message}");
        }
    }

    /// <summary>The name Explorer shows (e.g. "Google Chrome" for "Google Chrome.lnk").</summary>
    public static unsafe string GetDisplayName(string path)
    {
        try
        {
            PInvoke.SHCreateItemFromParsingName(path, null, out IShellItem item).ThrowOnFailure();
            try
            {
                item.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY, out var name);
                try
                {
                    return name.ToString();
                }
                finally
                {
                    Marshal.FreeCoTaskMem((nint)name.Value);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(item);
            }
        }
        catch (Exception exception) when (exception is COMException or IOException or UnauthorizedAccessException)
        {
            return Path.GetFileNameWithoutExtension(path);
        }
    }
}
