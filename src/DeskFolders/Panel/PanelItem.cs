using System.ComponentModel;
using System.Windows.Media.Imaging;
using DeskFolders.Diagnostics;
using DeskFolders.Folders;
using DeskFolders.Icons;

namespace DeskFolders.Panel;

/// <summary>One cell of a panel: an item's display name and its icon, which arrives later.</summary>
internal sealed class PanelItem(FolderItem item) : INotifyPropertyChanged
{
    private BitmapSource? _icon;

    public event PropertyChangedEventHandler? PropertyChanged;

    public FolderItem Item { get; } = item;

    public string Name { get; } = ShellItem.GetDisplayName(item.FullPath);

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
            Icon = await icons.GetIconAsync(Item);
        }
        catch (Exception exception)
        {
            Log.Error($"Icon of {Item.FullPath} failed", exception);
        }
    }
}
