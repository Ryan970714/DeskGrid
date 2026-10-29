using System.IO;
using DeskGrid.Config;
using DeskGrid.Diagnostics;
using DeskGrid.Resources;

namespace DeskGrid.Folders;

/// <summary>
/// Deletes a widget folder: with <see cref="DeleteMode.MoveToDesktop"/> its items are put on the desktop
/// first, then the folder itself goes to the Recycle Bin. If something can't be moved, the folder stays.
/// </summary>
internal static class FolderDeleter
{
    // Skips hidden and system entries (desktop.ini), like the scanner does.
    private static readonly EnumerationOptions VisibleEntries = new();

    /// <summary>The entries of <paramref name="folder"/> the user sees: files and subfolders, not hidden or system ones.</summary>
    public static List<string> Entries(string folder)
    {
        try
        {
            return [.. Directory.EnumerateFileSystemEntries(folder, "*", VisibleEntries)];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Cannot read {folder}", exception);
            return [];
        }
    }

    /// <summary>Deletes <paramref name="folder"/> as <paramref name="mode"/> says.</summary>
    /// <param name="recycle">Sends a path to the Recycle Bin; returns <see langword="false"/> if that was called off.</param>
    /// <returns><see langword="null"/> when the folder is gone, else why not.</returns>
    public static string? Delete(string folder, DeleteMode mode, string desktop, Func<string, bool> recycle)
    {
        if (mode == DeleteMode.MoveToDesktop && Entries(folder).Count > 0)
        {
            if (!Directory.Exists(desktop))
            {
                return Strings.DeleteNoDesktop;
            }

            if (MoveEntries(folder, desktop) is { Count: > 0 } stuck)
            {
                return string.Format(Strings.DeleteMoveFailed, string.Join(", ", stuck.Take(3).Select(Path.GetFileName)));
            }
        }

        try
        {
            return recycle(folder) ? null : Strings.DeleteStopped;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Deleting \"{folder}\" failed", exception);
            return string.Format(Strings.DeleteFailed, exception.Message);
        }
    }

    // Returns the entries that could not be moved.
    private static List<string> MoveEntries(string folder, string destination)
    {
        var stuck = new List<string>();
        foreach (var entry in Entries(folder))
        {
            try
            {
                var target = ItemImporter.UniquePath(destination, Path.GetFileName(entry));
                if (Directory.Exists(entry))
                {
                    Directory.Move(entry, target);
                }
                else
                {
                    File.Move(entry, target);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Log.Error($"Moving \"{entry}\" to the desktop failed", exception);
                stuck.Add(entry);
            }
        }

        return stuck;
    }
}
