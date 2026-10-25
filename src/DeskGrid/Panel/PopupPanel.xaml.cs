using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using System.Windows.Threading;
using DeskGrid.Desktop;
using DeskGrid.Folders;
using DeskGrid.Icons;
using DeskGrid.Resources;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DeskGrid.Panel;

/// <summary>
/// The grid of a folder's items that opens next to its widget. Fixed column count, scrolls past
/// <see cref="MaxVisibleRows"/> rows. Closes itself on Esc or when it loses activation. Items can be dragged
/// (the others slide out of the way like on a phone's home screen), and the folder's name can be edited.
/// </summary>
internal partial class PopupPanel : Window
{
    private const int MaxVisibleRows = 4;
    private const double ItemWidth = 76;
    private const double ItemHeight = 96;
    private const double IconCenterY = 28; // button padding 4 + half of the 48 DIP icon
    private const double AnchorGap = 8;

    // Marks a drag that started in a panel, so a panel can tell it from files dragged in from elsewhere.
    private const string ItemFormat = "DeskGrid.PanelItem";
    private const string FolderFormat = "DeskGrid.PanelFolder";

    private static readonly TimeSpan ResizeTime = TimeSpan.FromMilliseconds(180);
    private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(200));
    private static readonly IEasingFunction SlideEasing = new CubicEase { EasingMode = EasingMode.EaseOut };

    private readonly ScreenRect _anchor;
    private readonly PanelTheme _theme;
    private readonly IconService _icons;
    private readonly int _columns;
    private bool _closing;
    private bool _hiddenForDrag;
    private List<PanelItem> _items = [];
    private PanelItem? _pressed;
    private Point _pressPoint;
    private bool _dragging;
    private bool _reordered;
    private bool _renaming;
    private DispatcherTimer? _resize;

    // Dragging an item out of this panel: its index, the slot it would land in (-1 outside the panel), the
    // cursor-to-icon offset, and what to do once the drag is over.
    private int _dragIndex = -1;
    private int _slot = -1;
    private (double X, double Y) _grab;
    private DragGhost? _ghost;
    private ScreenPoint? _landing;
    private ItemReorderedEventArgs? _pendingReorder;

    // Something dragged over this panel from elsewhere: the slot left open for it, or -1.
    private int _gap = -1;

    public PopupPanel(WidgetFolder folder, ScreenRect anchor, int columns, PanelTheme theme, IconService icons)
    {
        InitializeComponent();
        _anchor = anchor;
        _theme = theme;
        _icons = icons;
        _columns = columns;
        Folder = folder;

        ApplyTheme();
        EmptyHint.Text = Strings.PanelEmpty;
        EditGlyph.ToolTip = Strings.PanelRename;
        Items.Width = columns * ItemWidth;
        Scroller.MaxHeight = MaxVisibleRows * ItemHeight;
        ShowFolder(folder);

        SourceInitialized += OnSourceInitialized;
        Closing += (_, _) => _closing = true;
        // While an item is being dragged the drop target (the desktop, another app) takes activation.
        Deactivated += (_, _) =>
        {
            if (_dragging)
            {
                return;
            }

            if (_renaming)
            {
                CommitRename(keepOpenOnError: false);
            }

            Dismiss();
        };
    }

    public event EventHandler<FolderItem>? ItemInvoked;

    public event EventHandler<ItemReorderedEventArgs>? ItemReordered;

    /// <summary>Items from another place were dropped here.</summary>
    public event EventHandler<ItemsDroppedEventArgs>? ItemsDropped;

    /// <summary>An item of this panel was dragged and let go (wherever it landed).</summary>
    public event EventHandler? DragFinished;

    /// <summary>Renames the folder on disk: old name, new name; returns why not, or <see langword="null"/> on success.</summary>
    public Func<string, string, string?>? Renamer { get; set; }

    public WidgetFolder Folder { get; private set; }

    /// <summary>
    /// Whether the panel takes the focus when it appears. Off for one that opens in the middle of a drag, where
    /// the drag keeps the focus; <see cref="Window.Activate"/> it once the drag is over.
    /// </summary>
    public bool ActivateWhenShown { get; set; } = true;

    /// <summary>Whether one of this panel's items is being dragged.</summary>
    public bool IsDragSource => _dragging;

    /// <summary>Shows <paramref name="folder"/>'s current items; also used to refresh an open panel.</summary>
    public void ShowFolder(WidgetFolder folder)
    {
        Folder = folder;
        Heading.Text = folder.Name;
        _items = [.. folder.Items.Select(i => new PanelItem(i))];
        Items.ItemsSource = _items;
        EmptyHint.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var item in _items)
        {
            _ = item.LoadIconAsync(_icons);
        }

        FitToContent();
        if (new WindowInteropHelper(this).Handle is not 0 and var window)
        {
            Reposition(new HWND(window));
        }
    }

    /// <summary>Closes the panel unless it is already closing.</summary>
    public void Dismiss()
    {
        if (!_closing)
        {
            Close();
        }
    }

    /// <summary>
    /// Another panel is about to open under the cursor while this panel's item is being dragged: this panel
    /// disappears, but stays alive until the drag is over, since the drag runs inside it.
    /// </summary>
    public void HideForDrag()
    {
        _hiddenForDrag = true;
        Backdrop.SetCloaked(new HWND(new WindowInteropHelper(this).Handle), true);
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        if (IsOwnItem(e))
        {
            var slot = SlotAt(e.GetPosition(Items), _items.Count - 1) ?? _slot;
            if (slot != _slot)
            {
                _slot = slot;
                Slide(landAt: slot);
            }

            e.Effects = DragDropEffects.Move;
        }
        else if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // From another panel or another app: open a gap where it would go.
            if (_gap < 0)
            {
                MakeRoomForIncoming(true);
            }

            var gap = SlotAt(e.GetPosition(Items), _items.Count) ?? (_gap < 0 ? _items.Count : _gap);
            if (gap != _gap)
            {
                _gap = gap;
                Slide(gapAt: gap);
            }

            e.Effects = (e.AllowedEffects & DragDropEffects.Copy) != 0 ? DragDropEffects.Copy : DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    protected override void OnDragLeave(DragEventArgs e)
    {
        // DragLeave also arrives from the children while the cursor is still inside the window.
        PInvoke.GetCursorPos(out var cursor);
        if (new Rect(RenderSize).Contains(PointFromScreen(new Point(cursor.X, cursor.Y))))
        {
            return;
        }

        if (_dragIndex >= 0)
        {
            // Taken out of the folder: it closes (the drag goes on, so the window only disappears).
            _slot = -1;
            HideForDrag();
        }
        else if (_gap >= 0)
        {
            _gap = -1;
            Slide(gapAt: null);
            MakeRoomForIncoming(false);
        }
    }

    protected override void OnDrop(DragEventArgs e)
    {
        if (IsOwnItem(e) && e.Data.GetData(ItemFormat) is string name)
        {
            _reordered = true;
            if (_slot >= 0)
            {
                _landing = SlotCenter(_slot);
                _pendingReorder = new ItemReorderedEventArgs(Folder.Name, name, _slot);
            }

            e.Effects = DragDropEffects.Move;
        }
        else if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths)
        {
            var index = _gap >= 0 ? _gap : _items.Count;
            _gap = -1;
            _resize?.Stop();
            Items.MinHeight = 0; // the refresh that follows sizes the window for the new item
            ItemsDropped?.Invoke(this, new ItemsDroppedEventArgs(Folder.Name, paths, index));
            FitToContent();

            // We moved or copied the files ourselves; never report Move, or the source would delete them again.
            e.Effects = DragDropEffects.Copy;
        }

        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Dismiss();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    private bool IsOwnItem(DragEventArgs e) =>
        _dragIndex >= 0 && e.Data.GetDataPresent(ItemFormat)
        && e.Data.GetData(FolderFormat) is string folder && string.Equals(folder, Folder.Name, StringComparison.OrdinalIgnoreCase);

    private void ApplyTheme()
    {
        Foreground = new SolidColorBrush(_theme.Foreground);
        Resources["HoverBrush"] = new SolidColorBrush(_theme.Hover);

        if (_theme.UseAcrylic)
        {
            // DWM only draws a system backdrop for windows with a frame. Keep the frame, but extend it
            // over the whole window (no visible non-client area); see OnSourceInitialized for the rest.
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(0),
                GlassFrameThickness = new Thickness(-1),
                UseAeroCaptionButtons = false,
            });
        }
        else
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Frame.CornerRadius = new CornerRadius(8);
            Frame.BorderThickness = new Thickness(1);
            Frame.BorderBrush = new SolidColorBrush(_theme.Outline);
            Frame.Background = new SolidColorBrush(_theme.Background);
        }
    }

    // Sized from the content rather than SizeToContent, which adds a non-client frame that
    // WindowChrome has removed and so leaves the window too big.
    private void FitToContent()
    {
        _resize?.Stop();
        Frame.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Width = Frame.DesiredSize.Width;
        Height = Frame.DesiredSize.Height;
    }

    // Makes room for one more item (a new row if the last one is full) while something is dragged over, or takes
    // the room back, growing or shrinking the window smoothly.
    private void MakeRoomForIncoming(bool incoming)
    {
        var rows = (_items.Count + (incoming ? 1 : 0) + _columns - 1) / _columns;
        var currentRows = Math.Max(1, (_items.Count + _columns - 1) / _columns);
        Items.MinHeight = incoming && rows > currentRows ? rows * ItemHeight : 0;
        Frame.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var from = Height;
        var to = Frame.DesiredSize.Height;
        if (Math.Abs(from - to) < 0.5)
        {
            return;
        }

        _resize?.Stop();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var timer = _resize = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(8) };
        timer.Tick += (_, _) =>
        {
            var t = Math.Min(1, clock.Elapsed / ResizeTime);
            Height = from + (to - from) * (1 - Math.Pow(1 - t, 3));
            if (new WindowInteropHelper(this).Handle is not 0 and var window)
            {
                // Keeps the panel on the side of its widget it opened on.
                Reposition(new HWND(window));
            }

            if (t >= 1)
            {
                timer.Stop();
            }
        };
        timer.Start();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var window = new HWND(new WindowInteropHelper(this).Handle);
        if (_theme.UseAcrylic)
        {
            HwndSource.FromHwnd(window).CompositionTarget.BackgroundColor = Colors.Transparent;

            // Without a system menu DWM draws no caption buttons in the extended frame.
            var style = (WINDOW_STYLE)PInvoke.GetWindowLong(window, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            PInvoke.SetWindowLong(window, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)(style & ~WINDOW_STYLE.WS_SYSMENU));

            if (Backdrop.TryApplyAcrylic(window, _theme.IsDark))
            {
                // The backdrop and the first frame of content arrive a moment apart; stay invisible until both
                // are there, then appear in one piece.
                Backdrop.SetCloaked(window, true);
                ContentRendered += OnFirstFrame;
            }
            else
            {
                Frame.Background = new SolidColorBrush(_theme.Background);
            }
        }

        // Before the first show, so the panel never flashes elsewhere.
        Reposition(window);
    }

    private void OnFirstFrame(object? sender, EventArgs e)
    {
        ContentRendered -= OnFirstFrame;
        // Let DWM compose that frame with the backdrop before the window is revealed.
        Dispatcher.BeginInvoke(() =>
        {
            if (!_closing && !_hiddenForDrag)
            {
                Backdrop.SetCloaked(new HWND(new WindowInteropHelper(this).Handle), false);
                if (ActivateWhenShown)
                {
                    Activate();
                }
            }
        }, DispatcherPriority.ApplicationIdle);
    }

    private void Reposition(HWND window)
    {
        var monitor = MonitorArea.At(new ScreenPoint((_anchor.Left + _anchor.Right) / 2, (_anchor.Top + _anchor.Bottom) / 2));
        var position = PanelPlacement.Place(_anchor, monitor.ToPixels(Width), monitor.ToPixels(Height),
            monitor.WorkArea, monitor.ToPixels(AnchorGap));
        PInvoke.SetWindowPos(window, HWND.Null, position.X, position.Y, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
            | SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
    }

    // ---- Renaming the folder ----

    private void OnHeadingEnter(object sender, MouseEventArgs e) => FadeGlyph(0.8);

    private void OnHeadingLeave(object sender, MouseEventArgs e) => FadeGlyph(0);

    private void FadeGlyph(double opacity) =>
        EditGlyph.BeginAnimation(OpacityProperty, new DoubleAnimation(_renaming ? 0 : opacity, new Duration(TimeSpan.FromMilliseconds(120))));

    private void OnHeadingClick(object sender, MouseButtonEventArgs e)
    {
        if (_renaming || Renamer is null)
        {
            return;
        }

        _renaming = true;
        HeadingEditor.Text = Folder.Name;
        HeadingEditor.ToolTip = null;
        Heading.Visibility = Visibility.Hidden;
        EditGlyph.BeginAnimation(OpacityProperty, null);
        EditGlyph.Opacity = 0;
        HeadingEditor.Visibility = Visibility.Visible;
        HeadingEditor.Focus();
        HeadingEditor.SelectAll();
        e.Handled = true;
    }

    private void OnEditorKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitRename(keepOpenOnError: true);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            EndRename();
            e.Handled = true;
        }
    }

    private void OnEditorLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (_renaming)
        {
            CommitRename(keepOpenOnError: false);
        }
    }

    private void CommitRename(bool keepOpenOnError)
    {
        var name = HeadingEditor.Text.Trim();
        if (name == Folder.Name || Renamer is null)
        {
            EndRename();
            return;
        }

        if (Renamer(Folder.Name, name) is { } problem)
        {
            if (keepOpenOnError)
            {
                HeadingEditor.ToolTip = new ToolTip { Content = problem, IsOpen = true, StaysOpen = false };
                HeadingEditor.SelectAll();
            }
            else
            {
                EndRename();
            }

            return;
        }

        // The folder is renamed on disk; follow it, so the rescan that comes with it still finds this panel's folder.
        Folder = Folder with { Name = name, FullPath = Path.Combine(Path.GetDirectoryName(Folder.FullPath) ?? "", name) };
        Heading.Text = name;
        EndRename();
    }

    private void EndRename()
    {
        _renaming = false;
        HeadingEditor.Visibility = Visibility.Collapsed;
        Heading.Visibility = Visibility.Visible;
        Keyboard.ClearFocus();
        Focus();
    }

    // ---- Dragging items ----

    private void OnItemPress(object sender, MouseButtonEventArgs e)
    {
        _pressed = ((FrameworkElement)sender).DataContext as PanelItem;
        _pressPoint = e.GetPosition(this);
    }

    private void OnItemRelease(object sender, MouseButtonEventArgs e) => _pressed = null;

    private void OnItemMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _pressed is not { } item)
        {
            return;
        }

        var offset = e.GetPosition(this) - _pressPoint;
        if (Math.Abs(offset.X) >= SystemParameters.MinimumHorizontalDragDistance
            || Math.Abs(offset.Y) >= SystemParameters.MinimumVerticalDragDistance)
        {
            _pressed = null;
            DragItem(item, (FrameworkElement)sender);
        }
    }

    // Drops on this panel reorder; anywhere else the target (desktop, another widget, a folder) takes the file.
    private void DragItem(PanelItem item, FrameworkElement source)
    {
        _dragIndex = _items.IndexOf(item);
        _slot = _dragIndex;
        _dragging = true;
        _reordered = false;
        _landing = null;
        _pendingReorder = null;

        // The icon lifts off its slot (which stays, empty) and follows the cursor.
        var origin = SlotCenter(_dragIndex);
        PInvoke.GetCursorPos(out var cursor);
        _grab = (origin.X - cursor.X, origin.Y - cursor.Y);
        _ghost = new DragGhost(item.Icon);
        _ghost.Show();
        _ghost.PlaceCenter(origin);
        _ghost.Lift();
        if (ContainerAt(_dragIndex) is { } container)
        {
            container.Opacity = 0;
        }

        var data = new DataObject(DataFormats.FileDrop, new[] { item.Item.FullPath });
        data.SetData(ItemFormat, item.Item.Name);
        data.SetData(FolderFormat, Folder.Name);

        DragDrop.AddQueryContinueDragHandler(source, OnQueryContinueDrag);
        DragDropEffects effect;
        try
        {
            effect = DragDrop.DoDragDrop(source, data, DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Link);
        }
        finally
        {
            DragDrop.RemoveQueryContinueDragHandler(source, OnQueryContinueDrag);
            _dragging = false;
        }

        FinishDrag(effect);
    }

    private void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
    {
        PInvoke.GetCursorPos(out var cursor);
        _ghost?.PlaceCenter(new ScreenPoint((int)Math.Round(cursor.X + _grab.X), (int)Math.Round(cursor.Y + _grab.Y)));
    }

    private void FinishDrag(DragDropEffects effect)
    {
        var ghost = _ghost;
        var index = _dragIndex;
        var landing = _landing;
        var reorder = _pendingReorder;
        _ghost = null;
        _dragIndex = -1;
        _slot = -1;
        _pendingReorder = null;

        if ((effect != DragDropEffects.None || _hiddenForDrag) && !_reordered)
        {
            // Taken elsewhere (or let go outside the closed folder): the icon shrinks into whatever it was dropped on.
            ghost?.Land(default, vanish: true);
        }
        else
        {
            // Settled in this panel, or let go over nothing: the icon flies to its slot.
            var settle = landing ?? SlotCenter(index);
            ghost?.Land(settle, vanish: false, RestoreItems);
        }

        if (reorder is not null)
        {
            ItemReordered?.Invoke(this, reorder);
        }

        DragFinished?.Invoke(this, EventArgs.Empty);

        // Gone to another place (or another app has the focus now): this panel has done its job.
        if (_hiddenForDrag || (!_reordered && (effect != DragDropEffects.None || !IsActive)))
        {
            Dismiss();
        }
    }

    // ---- Sliding the items ----

    private Point SlotOrigin(int slot) => new(slot % _columns * ItemWidth, slot / _columns * ItemHeight);

    private ScreenPoint SlotCenter(int slot)
    {
        var origin = SlotOrigin(slot);
        var point = Items.PointToScreen(new Point(origin.X + ItemWidth / 2, origin.Y + IconCenterY));
        return new ScreenPoint((int)Math.Round(point.X), (int)Math.Round(point.Y));
    }

    private FrameworkElement? ContainerAt(int index) => Items.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;

    // The slot under a point of the item list, or null if it is outside the list.
    private int? SlotAt(Point point, int last)
    {
        if (last < 0 || point.X < 0 || point.Y < 0 || point.X > Items.ActualWidth || point.Y > Items.ActualHeight + ItemHeight)
        {
            return last < 0 ? 0 : null;
        }

        var column = Math.Clamp((int)(point.X / ItemWidth), 0, _columns - 1);
        return Math.Clamp((int)(point.Y / ItemHeight) * _columns + column, 0, last);
    }

    /// <summary>
    /// Moves every item to where it belongs: with the dragged item taken out and, if <paramref name="landAt"/>
    /// is set, put back at that slot; or with a gap at <paramref name="gapAt"/> for something coming in.
    /// </summary>
    private void Slide(int? landAt = null, int? gapAt = null)
    {
        for (var index = 0; index < _items.Count; index++)
        {
            if (ContainerAt(index) is not { } container)
            {
                continue;
            }

            var slot = index;
            if (_dragIndex >= 0)
            {
                if (index == _dragIndex)
                {
                    slot = landAt ?? index;
                }
                else
                {
                    // Position among the others once the dragged one is out of the line.
                    var rank = index > _dragIndex ? index - 1 : index;
                    slot = landAt is { } land && rank >= land ? rank + 1 : rank;
                }
            }
            else if (gapAt is { } gap && index >= gap)
            {
                slot = index + 1;
            }

            var move = SlotOrigin(slot) - SlotOrigin(index);
            SlideTo(container, move.X, move.Y);
        }
    }

    private void RestoreItems()
    {
        for (var index = 0; index < _items.Count; index++)
        {
            if (ContainerAt(index) is { } container)
            {
                container.Opacity = 1;
                SlideTo(container, 0, 0);
            }
        }
    }

    private static void SlideTo(FrameworkElement element, double x, double y)
    {
        if (element.RenderTransform is not TranslateTransform { IsFrozen: false } transform)
        {
            transform = new TranslateTransform();
            element.RenderTransform = transform;
        }

        transform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(x, SlideDuration) { EasingFunction = SlideEasing });
        transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(y, SlideDuration) { EasingFunction = SlideEasing });
    }

    private void OnItemClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is PanelItem item)
        {
            ItemInvoked?.Invoke(this, item.Item);
        }
    }
}
