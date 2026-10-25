using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace DeskFolders.Shell;

/// <summary>Writes .lnk files. Call on an STA thread (the UI thread is one).</summary>
internal static class ShortcutFile
{
    /// <summary>Creates <paramref name="shortcutPath"/>, a shortcut that opens <paramref name="target"/> (a file or folder).</summary>
    public static unsafe void Create(string shortcutPath, string target)
    {
        var link = (IShellLinkW)new ShellLink();
        try
        {
            // Start in the target's own folder, as Explorer's "Create shortcut" does.
            fixed (char* targetPath = target, workingDirectory = Path.GetDirectoryName(target) ?? "", file = shortcutPath)
            {
                link.SetPath(targetPath);
                link.SetWorkingDirectory(workingDirectory);
                ((IPersistFile)link).Save(file, true);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(link);
        }
    }
}
