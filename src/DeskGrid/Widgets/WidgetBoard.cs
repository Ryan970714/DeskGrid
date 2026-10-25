using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DeskGrid.Config;
using DeskGrid.Desktop;
using DeskGrid.Folders;
using DeskGrid.Icons;

namespace DeskGrid.Widgets;

/// <param name="Bounds">The widget's cell on screen, in physical pixels.</param>
internal sealed record WidgetClickedEventArgs(WidgetFolder Folder, ScreenRect Bounds);

/// <summary>
/// Shows one <see cref="FolderWidget"/> per widget folder on the desktop and keeps them in step with
/// the catalog. Every widget sits on its own <see cref="WidgetGrid"/> cell; positions are saved.
/// Files dropped on a widget are added to its folder by <see cref="ItemImporter"/>.
/// </summary>
internal sealed class WidgetBoard(DesktopHost host, ConfigStore configStore, IconService icons) : IDisposable
{
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private DesktopIconGrid? _nativeIcons;

    public event EventHandler<WidgetClickedEventArgs>? WidgetClicked;

    /// <summary>Something being dragged stayed over a widget: its folder should open.</summary>
    public event EventHandler<WidgetClickedEventArgs>? DragHovered;

    /// <summary>The user chose "Pin to taskbar" on a widget.</summary>
    public event EventHandler<WidgetFolder>? PinRequested;

    public void Show(IReadOnlyList<WidgetFolder> folders)
    {
        _nativeIcons = host.ReadIconGrid();
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

        foreach (var entry in _entries.Values)
        {
            ApplyCellSize(entry);
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

    // Cells are exactly as big as the desktop's icon cells, so a widget looks and sits like a shortcut.
    private (int Width, int Height) CellSize(MonitorArea monitor) =>
        _nativeIcons is { } icons
            ? (icons.CellWidth, icons.CellHeight)
            : (monitor.ToPixels(FolderWidget.CellWidth), monitor.ToPixels(FolderWidget.CellHeight));

    private WidgetGrid GridOn(MonitorArea monitor)
    {
        var (width, height) = CellSize(monitor);
        return new WidgetGrid(monitor.WorkArea, width, height, _nativeIcons?.Anchor, _nativeIcons?.Icons);
    }

    // Returns whether the saved position changed.
    private bool Add(WidgetFolder folder)
    {
        var taken = TakenCells(except: null);
        var position = SavedPosition(folder.Name) is { } saved
            ? GridOn(MonitorArea.At(saved)).NearestFree(saved, taken)
            : GridOn(MonitorArea.Primary).FirstFree(taken);

        var entry = new Entry(this, folder, position);
        _entries[folder.Name] = entry;
        ApplyCellSize(entry);
        host.Add(entry.Surface);
        return SavePosition(folder.Name, position);
    }

    // A widget was dragged and let go: snap it to the nearest free cell.
    private void Snap(Entry entry)
    {
        _nativeIcons = host.ReadIconGrid();
        var dropped = entry.Surface.Position;
        var position = GridOn(MonitorArea.At(dropped)).NearestFree(dropped, TakenCells(except: entry));
        entry.Surface.MoveTo(position);
        if (SavePosition(entry.Folder.Name, position))
        {
            configStore.Save();
        }
    }

    private static string[] DroppedPaths(DragEventArgs e) =>
        e.Data.GetData(DataFormats.FileDrop) as string[] ?? [];

    // The cursor shows what the first droppable item would do.
    private DragDropEffects DropEffect(WidgetFolder folder, DragEventArgs e)
    {
        var action = DroppedPaths(e)
            .Select(path => ItemImporter.Plan(path, folder.FullPath, configStore.Config.DropMode, FolderPaths.Desktops, configStore.Config.RootPath))
            .FirstOrDefault(a => a != ImportAction.None);
        var effect = action switch
        {
            ImportAction.Move => DragDropEffects.Move,
            ImportAction.Copy => DragDropEffects.Copy,
            ImportAction.CreateShortcut => DragDropEffects.Link,
            _ => DragDropEffects.None,
        };

        // Show Copy if the source doesn't offer the natural effect; the drop is handled the same either way.
        return effect == DragDropEffects.None || (e.AllowedEffects & effect) != 0
            ? effect
            : e.AllowedEffects & DragDropEffects.Copy;
    }

    private void ImportDropped(WidgetFolder folder, DragEventArgs e)
    {
        var added = ItemImporter.Import(DroppedPaths(e), folder.FullPath, configStore.Config.DropMode, FolderPaths.Desktops, configStore.Config.RootPath);

        // We moved, copied or linked the files ourselves. Never report Move: the source would then
        // delete what it thinks it still has.
        e.Effects = added > 0 ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private ScreenRect BoundsOf(Entry entry)
    {
        var position = entry.Surface.Position;
        var (width, height) = CellSize(MonitorArea.At(position));
        return new ScreenRect(position.X, position.Y, position.X + width, position.Y + height);
    }

    // The view is laid out in DIP, the cell is in physical pixels.
    private void ApplyCellSize(Entry entry)
    {
        var monitor = MonitorArea.At(entry.Surface.Position);
        var (width, height) = CellSize(monitor);
        entry.View.SetCellSize(width * 96.0 / monitor.Dpi, height * 96.0 / monitor.Dpi);
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
        private static readonly TimeSpan HoverOpenDelay = TimeSpan.FromMilliseconds(550);

        private readonly DispatcherTimer _hover;
        private ScreenPoint _dragOrigin;

        public Entry(WidgetBoard board, WidgetFolder folder, ScreenPoint position)
        {
            Folder = folder;
            View = new FolderWidget();
            Surface = new DesktopSurface(View, position);
            _hover = new DispatcherTimer { Interval = HoverOpenDelay };

            View.Clicked += (_, _) => board.WidgetClicked?.Invoke(board, new WidgetClickedEventArgs(Folder, board.BoundsOf(this)));
            View.PinRequested += (_, _) => board.PinRequested?.Invoke(board, Folder);
            View.DragStarted += (_, _) => _dragOrigin = Surface.Position;
            View.Dragged += (_, offset) => Surface.MoveTo(_dragOrigin.Offset(offset.X, offset.Y));
            View.DragCompleted += (_, _) => board.Snap(this);
            View.DragOver += (_, e) =>
            {
                e.Effects = board.DropEffect(Folder, e);
                e.Handled = true;
            };
            View.Drop += (_, e) =>
            {
                _hover.Stop();
                board.ImportDropped(Folder, e);
                e.Handled = true;
            };

            // Held over a widget for a moment, a drag opens its folder, so the item can be put inside.
            _hover.Tick += (_, _) =>
            {
                _hover.Stop();
                board.DragHovered?.Invoke(board, new WidgetClickedEventArgs(Folder, board.BoundsOf(this)));
            };
            View.DragEnter += (_, e) =>
            {
                if (board.DropEffect(Folder, e) != DragDropEffects.None)
                {
                    _hover.Start();
                }
            };
            View.DragLeave += (_, _) => _hover.Stop();
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
