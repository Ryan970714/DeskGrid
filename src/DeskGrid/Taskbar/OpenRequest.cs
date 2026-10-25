namespace DeskGrid.Taskbar;

/// <summary>
/// The command line of a taskbar shortcut: <c>DeskGrid.exe --open "Folder"</c> asks the running DeskGrid
/// to open that folder's panel.
/// </summary>
internal static class OpenRequest
{
    private const string Switch = "--open";

    public static string Arguments(string folderName) => $"{Switch} \"{folderName}\"";

    /// <summary>The folder name asked for, or <see langword="null"/> if <paramref name="args"/> isn't an open request.</summary>
    public static string? Parse(IReadOnlyList<string> args) =>
        args is [var option, var name, ..] && string.Equals(option, Switch, StringComparison.OrdinalIgnoreCase) && name.Length > 0
            ? name
            : null;
}
