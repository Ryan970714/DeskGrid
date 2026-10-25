namespace DeskGrid.Panel;

/// <summary>An item was dragged to another place within its panel.</summary>
/// <param name="Index">Where the item belongs in the folder's item order.</param>
internal sealed record ItemReorderedEventArgs(string FolderName, string ItemName, int Index);
