using System.IO;

namespace DeskFolders.Folders;

/// <summary>Path comparisons the way the file system sees them: full paths, case-insensitive.</summary>
internal static class FolderPaths
{
    /// <summary>The folders whose contents Explorer shows as desktop icons: the user's desktop and the public one.</summary>
    public static IReadOnlyList<string> Desktops { get; } =
    [
        .. new[] { Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolder.CommonDesktopDirectory }
            .Select(Environment.GetFolderPath)
            .Where(path => path.Length > 0),
    ];

    public static bool IsSameOrInside(string path, string folder) =>
        WithTrailingSeparator(path).StartsWith(WithTrailingSeparator(folder), StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether <paramref name="path"/> is an entry of <paramref name="folder"/> itself, not of a subfolder.</summary>
    public static bool IsDirectlyIn(string path, string folder) =>
        Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(path))) is { } parent
        && string.Equals(WithTrailingSeparator(parent), WithTrailingSeparator(folder), StringComparison.OrdinalIgnoreCase);

    private static string WithTrailingSeparator(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Path.EndsInDirectorySeparator(fullPath) ? fullPath : fullPath + Path.DirectorySeparatorChar;
    }
}
