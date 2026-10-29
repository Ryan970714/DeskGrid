using System.IO;
using DeskGrid.Shell;

namespace DeskGrid.Folders;

/// <summary>Finds the file behind a desktop icon from the name Explorer shows under it.</summary>
internal static class DesktopFiles
{
    // Skips hidden and system entries, which Explorer doesn't show either.
    private static readonly EnumerationOptions VisibleEntries = new();

    /// <summary>
    /// The files and folders of the <paramref name="desktops"/> by the name Explorer shows for them. A name that
    /// belongs to several (the same name on the user's and the public desktop, or "a.txt" and "a.lnk" with
    /// extensions hidden) is left out: the icon can't be told apart.
    /// </summary>
    public static Dictionary<string, string> ByDisplayName(IEnumerable<string> desktops) =>
        ByDisplayName(
            desktops.Where(Directory.Exists).SelectMany(desktop => Directory.EnumerateFileSystemEntries(desktop, "*", VisibleEntries)),
            ShellItem.GetDisplayName);

    internal static Dictionary<string, string> ByDisplayName(IEnumerable<string> paths, Func<string, string> displayName) =>
        paths
            .GroupBy(displayName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);
}
