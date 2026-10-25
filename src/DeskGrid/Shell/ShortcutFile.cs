using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace DeskGrid.Shell;

/// <summary>Writes .lnk files. Call on an STA thread (the UI thread is one).</summary>
internal static class ShortcutFile
{
    /// <summary>
    /// Creates <paramref name="shortcutPath"/>, a shortcut that opens <paramref name="target"/> (a file or folder),
    /// or starts it with <paramref name="arguments"/> if it is a program, showing <paramref name="iconPath"/> if given.
    /// </summary>
    public static unsafe void Create(string shortcutPath, string target, string? arguments = null, string? iconPath = null)
    {
        var link = (IShellLinkW)new ShellLink();
        try
        {
            // Start in the target's own folder, as Explorer's "Create shortcut" does.
            fixed (char* targetPath = target, workingDirectory = Path.GetDirectoryName(target) ?? "", file = shortcutPath)
            fixed (char* argumentText = arguments, iconFile = iconPath)
            {
                link.SetPath(targetPath);
                link.SetWorkingDirectory(workingDirectory);
                if (arguments is not null)
                {
                    link.SetArguments(argumentText);
                }

                if (iconPath is not null)
                {
                    link.SetIconLocation(iconFile, 0);
                }

                ((IPersistFile)link).Save(file, true);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(link);
        }
    }
}
