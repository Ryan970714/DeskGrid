namespace DeskGrid.Panel;

/// <summary>An item was dragged to another place within its panel.</summary>
/// <param name="FolderName">The folder the item is in, or <see cref="FolderList"/> if a folder was moved in the list of all folders.</param>
/// <param name="Index">Where the item belongs in the folder's item order (or the folder in the folder order).</param>
internal sealed record ItemReorderedEventArgs(string FolderName, string ItemName, int Index)
{
    /// <summary>Not a valid folder name, so it can't clash with one.</summary>
    public const string FolderList = "*";
}
