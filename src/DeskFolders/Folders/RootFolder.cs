using System.IO;
using DeskFolders.Resources;

namespace DeskFolders.Folders;

/// <summary>Rules for the folder whose subfolders become widgets.</summary>
internal static class RootFolder
{
    /// <summary>
    /// Creates <paramref name="path"/> if needed. Returns <see langword="null"/> if it is usable as the
    /// root folder, otherwise a user-facing reason why not.
    /// </summary>
    public static string? Prepare(string path)
    {
        if (!Path.IsPathFullyQualified(path))
        {
            return Strings.RootFolderNotAbsolute;
        }

        if (IsOnDesktop(path))
        {
            return Strings.RootFolderOnDesktop;
        }

        try
        {
            Directory.CreateDirectory(path);
            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return exception.Message;
        }
    }

    // Explorer already shows these folders' contents as desktop icons.
    internal static bool IsOnDesktop(string path) =>
        new[] { Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolder.CommonDesktopDirectory }
            .Select(Environment.GetFolderPath)
            .Any(desktop => desktop.Length > 0 && IsSameOrInside(path, desktop));

    private static bool IsSameOrInside(string path, string folder) =>
        WithTrailingSeparator(path).StartsWith(WithTrailingSeparator(folder), StringComparison.OrdinalIgnoreCase);

    private static string WithTrailingSeparator(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Path.EndsInDirectorySeparator(fullPath) ? fullPath : fullPath + Path.DirectorySeparatorChar;
    }
}
