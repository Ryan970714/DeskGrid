using Windows.Win32;

namespace DeskFolders.Folders;

/// <summary>Explorer's name order: case-insensitive, digits compared by value ("2" before "10").</summary>
internal sealed class NaturalStringComparer : IComparer<string>
{
    public static NaturalStringComparer Instance { get; } = new();

    public int Compare(string? x, string? y) => PInvoke.StrCmpLogical(x ?? "", y ?? "");
}
