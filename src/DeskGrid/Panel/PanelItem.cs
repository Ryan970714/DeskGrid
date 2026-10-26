using System.ComponentModel;
using System.Windows.Media.Imaging;
using DeskGrid.Diagnostics;
using DeskGrid.Folders;
using DeskGrid.Icons;
using DeskGrid.Shell;

namespace DeskGrid.Panel;

/// <summary>
/// One cell of a panel: an item of a folder, or (in the list of all folders) a folder with its preview. Shows a
/// name and an icon, which arrives later.
/// </summary>
internal sealed class PanelItem : INotifyPropertyChanged
{
    private BitmapSource? _icon;

    public PanelItem(FolderItem item)
    {
        Item = item;
        Name = ShellItem.GetDisplayName(item.FullPath);
    }

    public PanelItem(WidgetFolder folder)
    {
        Folder = folder;
        Name = folder.Name;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The file this cell stands for, or <see langword="null"/> for a folder.</summary>
    public FolderItem? Item { get; }

    /// <summary>The folder this cell stands for, or <see langword="null"/> for a file.</summary>
    public WidgetFolder? Folder { get; }

    public bool IsFolder => Folder is not null;

    public string Name { get; }

    public BitmapSource? Icon
    {
        get => _icon;
        private set
        {
            _icon = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
        }
    }

    public async Task LoadIconAsync(IconService icons)
    {
        try
        {
            Icon = Folder is { } folder ? await icons.GetPreviewAsync(folder) : await icons.GetIconAsync(Item!);
        }
        catch (Exception exception)
        {
            Log.Error($"Icon of {Item?.FullPath ?? Folder?.FullPath} failed", exception);
        }
    }
}
