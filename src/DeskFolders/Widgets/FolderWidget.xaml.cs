using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeskFolders.Desktop;
using DeskFolders.Diagnostics;
using DeskFolders.Resources;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DeskFolders.Widgets;

/// <summary>
/// A folder on the desktop: the 2×2 preview on a translucent tile, with the folder name below.
/// Turns mouse input into a click or a drag; moving the window is up to the owner.
/// </summary>
internal partial class FolderWidget : UserControl
{
    /// <summary>The widget's footprint, which is also the grid cell it snaps to (DIP).</summary>
    public const double CellWidth = 76;
    public const double CellHeight = 96;

    private const int MaxLabelLines = 2;
    private static readonly Brush HoverBrush = Frozen(new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)));

    private Task<BitmapSource>? _preview;
    private ScreenPoint _pressPoint;
    private bool _dragging;

    public FolderWidget()
    {
        InitializeComponent();
        Width = CellWidth;
        Height = CellHeight;
        Label.MaxHeight = Label.FontSize * Label.FontFamily.LineSpacing * MaxLabelLines;

        // Temporary way out until the tray icon exists (stage 6).
        var exit = new MenuItem { Header = Strings.MenuExit };
        exit.Click += (_, _) => Application.Current.Shutdown();
        ContextMenu = new ContextMenu { Items = { exit } };
    }

    public event EventHandler? Clicked;

    public event EventHandler? DragStarted;

    /// <summary>Cursor offset from where the drag began, in physical pixels.</summary>
    public event EventHandler<ScreenPoint>? Dragged;

    public event EventHandler? DragCompleted;

    public void Show(string name, Task<BitmapSource> preview)
    {
        Label.Text = name;
        if (preview != _preview)
        {
            _preview = preview;
            _ = LoadPreviewAsync(preview);
        }
    }

    protected override void OnMouseEnter(MouseEventArgs e) => Highlight.Background = HoverBrush;

    protected override void OnMouseLeave(MouseEventArgs e) => Highlight.Background = null;

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        _pressPoint = CursorPosition();
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!IsMouseCaptured)
        {
            return;
        }

        var cursor = CursorPosition();
        var offset = new ScreenPoint(cursor.X - _pressPoint.X, cursor.Y - _pressPoint.Y);
        if (!_dragging && IsBeyondDragThreshold(offset))
        {
            _dragging = true;
            DragStarted?.Invoke(this, EventArgs.Empty);
        }

        if (_dragging)
        {
            Dragged?.Invoke(this, offset);
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (!IsMouseCaptured)
        {
            return;
        }

        var wasDragging = _dragging;
        _dragging = false;
        ReleaseMouseCapture();
        (wasDragging ? DragCompleted : Clicked)?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        // Capture taken away mid-drag (e.g. a window popped up): finish where we are.
        if (_dragging)
        {
            _dragging = false;
            DragCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private static ScreenPoint CursorPosition()
    {
        PInvoke.GetCursorPos(out var point);
        return new ScreenPoint(point.X, point.Y);
    }

    private static bool IsBeyondDragThreshold(ScreenPoint offset) =>
        Math.Abs(offset.X) >= PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXDRAG)
        || Math.Abs(offset.Y) >= PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYDRAG);

    private static T Frozen<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }

    private async Task LoadPreviewAsync(Task<BitmapSource> preview)
    {
        try
        {
            var image = await preview;
            if (preview == _preview)
            {
                Preview.Source = image;
            }
        }
        catch (Exception exception)
        {
            Log.Error($"Preview of \"{Label.Text}\" failed", exception);
        }
    }
}
