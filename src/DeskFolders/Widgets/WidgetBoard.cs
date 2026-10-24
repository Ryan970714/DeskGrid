using System.Windows.Media.Imaging;
using DeskFolders.Config;
using DeskFolders.Desktop;
using DeskFolders.Folders;
using DeskFolders.Icons;

namespace DeskFolders.Widgets;

/// <param name="Bounds">The widget's cell on screen, in physical pixels.</param>
internal sealed record WidgetClickedEventArgs(WidgetFolder Folder, ScreenRect Bounds);

/// <summary>
/// Shows one <see cref="FolderWidget"/> per widget folder on the desktop and keeps them in step with
/// the catalog. Every widget sits on its own <see cref="WidgetGrid"/> cell; positions are saved.
/// </summary>
internal sealed class WidgetBoard(DesktopHost host, ConfigStore configStore, IconService icons) : IDisposable
{
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<WidgetClickedEventArgs>? WidgetClicked;

    public void Show(IReadOnlyList<WidgetFolder> folders)
    {
        var present = folders.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in _entries.Keys.Where(n => !present.Contains(n)).ToList())
        {
            host.Remove(_entries[name].Surface);
            _entries.Remove(name);
        }

        // Widgets with a saved position go first, so new ones fill the cells around them.
        var configChanged = false;
        foreach (var folder in folders.Where(f => !_entries.ContainsKey(f.Name)).OrderBy(f => SavedPosition(f.Name) is null))
        {
            configChanged |= Add(folder);
        }

        foreach (var folder in folders)
        {
            _entries[folder.Name].Update(folder, icons.GetPreviewAsync(folder));
        }

        if (configChanged)
        {
            configStore.Save();
        }
    }

    public void Dispose()
    {
        foreach (var entry in _entries.Values)
        {
            host.Remove(entry.Surface);
        }

        _entries.Clear();
    }

    private static WidgetGrid GridOn(MonitorArea monitor) =>
        new(monitor.WorkArea, monitor.ToPixels(FolderWidget.CellWidth), monitor.ToPixels(FolderWidget.CellHeight));

    // Returns whether the saved position changed.
    private bool Add(WidgetFolder folder)
    {
        var taken = TakenCells(except: null);
        var position = SavedPosition(folder.Name) is { } saved
            ? GridOn(MonitorArea.At(saved)).NearestFree(saved, taken)
            : GridOn(MonitorArea.Primary).FirstFree(taken);

        var entry = new Entry(this, folder, position);
        _entries[folder.Name] = entry;
        host.Add(entry.Surface);
        return SavePosition(folder.Name, position);
    }

    private void Drop(Entry entry)
    {
        var dropped = entry.Surface.Position;
        var position = GridOn(MonitorArea.At(dropped)).NearestFree(dropped, TakenCells(except: entry));
        entry.Surface.MoveTo(position);
        if (SavePosition(entry.Folder.Name, position))
        {
            configStore.Save();
        }
    }

    private static ScreenRect BoundsOf(Entry entry)
    {
        var position = entry.Surface.Position;
        var monitor = MonitorArea.At(position);
        return new ScreenRect(position.X, position.Y,
            position.X + monitor.ToPixels(FolderWidget.CellWidth), position.Y + monitor.ToPixels(FolderWidget.CellHeight));
    }

    private HashSet<ScreenPoint> TakenCells(Entry? except) =>
        _entries.Values.Where(e => e != except).Select(e => e.Surface.Position).ToHashSet();

    private ScreenPoint? SavedPosition(string name) =>
        configStore.Config.Widgets.TryGetValue(name, out var widget) ? widget.Position : null;

    private bool SavePosition(string name, ScreenPoint position)
    {
        if (!configStore.Config.Widgets.TryGetValue(name, out var widget) || widget.Position == position)
        {
            return false;
        }

        widget.Position = position;
        return true;
    }

    /// <summary>One widget: its folder, its view and the desktop window hosting the view.</summary>
    private sealed class Entry
    {
        private ScreenPoint _dragOrigin;

        public Entry(WidgetBoard board, WidgetFolder folder, ScreenPoint position)
        {
            Folder = folder;
            View = new FolderWidget();
            Surface = new DesktopSurface(View, position);

            View.Clicked += (_, _) => board.WidgetClicked?.Invoke(board, new WidgetClickedEventArgs(Folder, BoundsOf(this)));
            View.DragStarted += (_, _) => _dragOrigin = Surface.Position;
            View.Dragged += (_, offset) => Surface.MoveTo(_dragOrigin.Offset(offset.X, offset.Y));
            View.DragCompleted += (_, _) => board.Drop(this);
        }

        public WidgetFolder Folder { get; private set; }

        public FolderWidget View { get; }

        public DesktopSurface Surface { get; }

        public void Update(WidgetFolder folder, Task<BitmapSource> preview)
        {
            Folder = folder;
            View.Show(folder.Name, preview);
        }
    }
}
