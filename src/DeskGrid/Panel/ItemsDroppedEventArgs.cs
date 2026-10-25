namespace DeskGrid.Panel;

/// <summary>Files or items from elsewhere were dropped on a panel, at <paramref name="Index"/> in its item list.</summary>
internal sealed record ItemsDroppedEventArgs(string FolderName, IReadOnlyList<string> Paths, int Index);
