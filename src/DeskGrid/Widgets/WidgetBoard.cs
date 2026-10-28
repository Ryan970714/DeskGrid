using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DeskGrid.Config;
using DeskGrid.Desktop;
using DeskGrid.Diagnostics;
using DeskGrid.Folders;
using DeskGrid.Icons;
using DeskGrid.Resources;

namespace DeskGrid.Widgets;

/// <param name="Bounds">The widget's cell on screen, in physical pixels.</param>
internal sealed record WidgetClickedEventArgs(WidgetFolder Folder, ScreenRect Bounds);

/// <summary>
/// Shows one <see cref="FolderWidget"/> per widget folder on the desktop and keeps them in step with
/// the catalog. Every widget sits on its own <see cref="WidgetGrid"/> cell; positions are saved.
/// Files dropped on a widget are added to its folder by <see cref="ItemImporter"/>;
/// files dropped on the "new folder" tile go into a folder created for them.
/// </summary>
internal sealed class WidgetBoard(DesktopHost host, ConfigStore configStore, IconService icons) : IDisposable
{
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private AddTile? _addTile;
    private DesktopIconGrid? _nativeIcons;

    public event EventHandler<WidgetClickedEventArgs>? WidgetClicked;

    /// <summary>Something being dragged stayed over a widget: its folder should open.</summary>
    public event EventHandler<WidgetClickedEventArgs>? DragHovered;

    /// <summary>The user chose "Pin to taskbar" on a widget.</summary>
    public event EventHandler<WidgetFolder>? PinRequested;

    /// <summary>The "new folder" tile was clicked.</summary>
    public event EventHandler? AddRequested;

    public void Show(IReadOnlyList<WidgetFolder> folders)
    {
        _nativeIcons = host.ReadIconGrid();
        var present = folders.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in _entries.Keys.Where(n => !present.Contains(n)).ToList())
        {
            _entries[name].Release(host);
            _entries.Remove(name);
        }

        // Widgets with a saved position go first, so new ones fill the cells around them.
        var configChanged = false;
        foreach (var folder in folders.Where(f => !_entries.ContainsKey(f.Name)).OrderBy(f => SavedPosition(f.Name) is null))
        {
            configChanged |= Add(folder);
        }

        configChanged |= ShowAddTile();

        foreach (var entry in _entries.Values)
        {
            ApplyCellSize(entry.View, entry.Surface);
        }

        if (_addTile is { } tile)
        {
            ApplyCellSize(tile.View, tile.Surface);
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
            entry.Release(host);
        }

        _entries.Clear();

        if (_addTile is { } tile)
        {
            host.Remove(tile.Surface);
            _addTile = null;
        }
    }

    // Cells are exactly as big as the desktop's icon cells, so a widget looks and sits like a shortcut.
    private (int Width, int Height) CellSize(MonitorArea monitor) =>
        _nativeIcons is { } icons
            ? (icons.CellWidth, icons.CellHeight)
            : (monitor.ToPixels(FolderWidget.CellWidth), monitor.ToPixels(FolderWidget.CellHeight));

    private WidgetGrid GridOn(MonitorArea monitor)
    {
        var (width, height) = CellSize(monitor);
        return new WidgetGrid(monitor.WorkArea, width, height, _nativeIcons?.Anchor, _nativeIcons?.Icons.Select(i => i.Cell).ToList());
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
        ApplyCellSize(entry.View, entry.Surface);
        host.Add(entry.Surface);
        return SavePosition(folder.Name, position);
    }

    /// <summary>
    /// Desktop icons changed: an icon that ended up on a widget (a new shortcut Explorer put in the first
    /// cell it thinks is free, or one the user dragged there) moves to the nearest free cell. When Explorer
    /// arranges the icons itself they can't be moved, so the widgets make way instead.
    /// </summary>
    public void ClearIconsOffWidgets()
    {
        _nativeIcons = host.ReadIconGrid();
        if (_nativeIcons is not { } grid)
        {
            return;
        }

        var placed = Placed().ToList();
        var widgetCells = placed.Select(p => BoundsOf(p.Surface)).ToList();
        var covering = grid.Icons.Where(i => widgetCells.Any(cell => WidgetGrid.Covers(i.Cell, cell))).ToList();
        if (covering.Count == 0)
        {
            return;
        }

        if (grid.AutoArranged)
        {
            var configChanged = false;
            foreach (var (surface, savePosition) in placed.Where(p => covering.Any(i => WidgetGrid.Covers(i.Cell, BoundsOf(p.Surface)))))
            {
                configChanged |= Place(surface, savePosition);
            }

            if (configChanged)
            {
                configStore.Save();
            }

            return;
        }

        // What a moved icon must stay off: the widgets and the icons staying where they are.
        var occupied = widgetCells.Concat(grid.Icons.Except(covering).Select(i => i.Cell)).ToList();
        foreach (var icon in covering)
        {
            var from = new ScreenPoint(icon.Cell.Left, icon.Cell.Top);
            var to = new WidgetGrid(MonitorArea.At(from).WorkArea, grid.CellWidth, grid.CellHeight, grid.Anchor, occupied)
                .NearestFree(from, taken: new HashSet<ScreenPoint>());
            var target = new ScreenRect(to.X, to.Y, to.X + grid.CellWidth, to.Y + grid.CellHeight);
            if (occupied.Any(o => WidgetGrid.Covers(o, target)))
            {
                continue; // The desktop is full.
            }

            host.MoveIcon(icon, to);
            occupied.Add(target);
        }
    }

    // A widget was dragged and let go: snap it to the nearest free cell.
    private void Snap(DesktopSurface surface, Func<ScreenPoint, bool> savePosition)
    {
        _nativeIcons = host.ReadIconGrid();
        if (Place(surface, savePosition))
        {
            configStore.Save();
        }
    }

    // Moves the widget to the free cell nearest to where it is. Returns whether its saved position changed.
    private bool Place(DesktopSurface surface, Func<ScreenPoint, bool> savePosition)
    {
        var current = surface.Position;
        var position = GridOn(MonitorArea.At(current)).NearestFree(current, TakenCells(except: surface));
        surface.MoveTo(position);
        return savePosition(position);
    }

    // The tile that creates a folder for whatever is dropped on it. Returns whether the saved position changed.
    private bool ShowAddTile()
    {
        if (_addTile is not null)
        {
            return false;
        }

        var saved = configStore.Config.AddTilePosition;
        var taken = TakenCells(except: null);
        var position = saved is { } s
            ? GridOn(MonitorArea.At(s)).NearestFree(s, taken)
            : GridOn(MonitorArea.Primary).FirstFree(taken);

        var view = new FolderWidget();
        view.ShowAddTile(Strings.WidgetAddTile);
        var surface = new DesktopSurface(view, position);
        var dragOrigin = position;
        view.Clicked += (_, _) => AddRequested?.Invoke(this, EventArgs.Empty);
        view.DragStarted += (_, _) => dragOrigin = surface.Position;
        view.Dragged += (_, offset) => surface.MoveTo(dragOrigin.Offset(offset.X, offset.Y));
        view.DragCompleted += (_, _) => Snap(surface, SaveAddTilePosition);
        view.DragOver += (_, e) =>
        {
            e.Effects = DropEffect(NewFolderPath(), e);
            e.Handled = true;
        };
        view.Drop += (_, e) =>
        {
            DropOnAddTile(e);
            e.Handled = true;
        };

        _addTile = new AddTile(view, surface);
        host.Add(surface);
        return SaveAddTilePosition(position);
    }

    private bool SaveAddTilePosition(ScreenPoint position)
    {
        if (configStore.Config.AddTilePosition == position)
        {
            return false;
        }

        configStore.Config.AddTilePosition = position;
        return true;
    }

    // Where the folder for a drop on the tile would be created.
    private string NewFolderPath()
    {
        var root = configStore.Config.RootPath;
        return Path.Combine(root, WidgetFolderName.Suggest(root));
    }

    private void DropOnAddTile(DragEventArgs e)
    {
        var folder = NewFolderPath();
        if (DropEffect(folder, e) == DragDropEffects.None)
        {
            e.Effects = DragDropEffects.None;
            return;
        }

        try
        {
            // The watcher turns it into a widget.
            Directory.CreateDirectory(folder);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log.Error("Creating a widget folder for a drop failed", exception);
            e.Effects = DragDropEffects.None;
            return;
        }

        ImportDropped(folder, e);
    }

    private static string[] DroppedPaths(DragEventArgs e) =>
        e.Data.GetData(DataFormats.FileDrop) as string[] ?? [];

    // The cursor shows what the first droppable item would do.
    private DragDropEffects DropEffect(string folderPath, DragEventArgs e)
    {
        var action = DroppedPaths(e)
            .Select(path => ItemImporter.Plan(path, folderPath, configStore.Config.DropMode, FolderPaths.Desktops, configStore.Config.RootPath))
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

    private void ImportDropped(string folderPath, DragEventArgs e)
    {
        var added = ItemImporter.Import(DroppedPaths(e), folderPath, configStore.Config.DropMode, FolderPaths.Desktops, configStore.Config.RootPath);

        // We moved, copied or linked the files ourselves. Never report Move: the source would then
        // delete what it thinks it still has.
        e.Effects = added > 0 ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private ScreenRect BoundsOf(DesktopSurface surface)
    {
        var position = surface.Position;
        var (width, height) = CellSize(MonitorArea.At(position));
        return new ScreenRect(position.X, position.Y, position.X + width, position.Y + height);
    }

    // The view is laid out in DIP, the cell is in physical pixels.
    private void ApplyCellSize(FolderWidget view, DesktopSurface surface)
    {
        var monitor = MonitorArea.At(surface.Position);
        var (width, height) = CellSize(monitor);
        view.SetCellSize(width * 96.0 / monitor.Dpi, height * 96.0 / monitor.Dpi);
    }

    private HashSet<ScreenPoint> TakenCells(DesktopSurface? except) =>
        Placed().Select(p => p.Surface).Where(s => s != except).Select(s => s.Position).ToHashSet();

    // Every widget and the "new folder" tile, with how to save each one's position.
    private IEnumerable<(DesktopSurface Surface, Func<ScreenPoint, bool> SavePosition)> Placed()
    {
        foreach (var entry in _entries.Values)
        {
            yield return (entry.Surface, position => SavePosition(entry.Folder.Name, position));
        }

        if (_addTile is { } tile)
        {
            yield return (tile.Surface, SaveAddTilePosition);
        }
    }

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

    private sealed record AddTile(FolderWidget View, DesktopSurface Surface);

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

            View.Clicked += (_, _) => board.WidgetClicked?.Invoke(board, new WidgetClickedEventArgs(Folder, board.BoundsOf(Surface)));
            View.PinRequested += (_, _) => board.PinRequested?.Invoke(board, Folder);
            View.DragStarted += (_, _) => _dragOrigin = Surface.Position;
            View.Dragged += (_, offset) => Surface.MoveTo(_dragOrigin.Offset(offset.X, offset.Y));
            View.DragCompleted += (_, _) => board.Snap(Surface, position => board.SavePosition(Folder.Name, position));
            View.DragOver += (_, e) =>
            {
                e.Effects = board.DropEffect(Folder.FullPath, e);
                e.Handled = true;
            };
            View.Drop += (_, e) =>
            {
                _hover.Stop();
                board.ImportDropped(Folder.FullPath, e);
                e.Handled = true;
            };

            // Held over a widget for a moment, a drag opens its folder, so the item can be put inside.
            _hover.Tick += (_, _) =>
            {
                _hover.Stop();
                board.DragHovered?.Invoke(board, new WidgetClickedEventArgs(Folder, board.BoundsOf(Surface)));
            };
            View.DragEnter += (_, e) =>
            {
                if (board.DropEffect(Folder.FullPath, e) != DragDropEffects.None)
                {
                    _hover.Start();
                }
            };
            View.DragLeave += (_, _) => _hover.Stop();
        }

        public WidgetFolder Folder { get; private set; }

        public FolderWidget View { get; }

        public DesktopSurface Surface { get; }

        // Takes the widget off the desktop; a pending "hold to open" must not fire for it afterwards.
        public void Release(DesktopHost host)
        {
            _hover.Stop();
            host.Remove(Surface);
        }

        public void Update(WidgetFolder folder, Task<BitmapSource> preview)
        {
            Folder = folder;
            View.Show(folder.Name, preview);
        }
    }
}
