using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using DeskFolders.Desktop;
using DeskFolders.Folders;
using DeskFolders.Icons;
using DeskFolders.Resources;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DeskFolders.Panel;

/// <summary>
/// The grid of a folder's items that opens next to its widget. Fixed column count, scrolls past
/// <see cref="MaxVisibleRows"/> rows. Closes itself on Esc or when it loses activation.
/// </summary>
internal partial class PopupPanel : Window
{
    private const int MaxVisibleRows = 4;
    private const double ItemWidth = 76;
    private const double ItemHeight = 96;
    private const double AnchorGap = 8;

    private readonly ScreenRect _anchor;
    private readonly PanelTheme _theme;
    private readonly IconService _icons;
    private bool _closing;

    public PopupPanel(WidgetFolder folder, ScreenRect anchor, int columns, PanelTheme theme, IconService icons)
    {
        InitializeComponent();
        _anchor = anchor;
        _theme = theme;
        _icons = icons;
        Folder = folder;

        ApplyTheme();
        EmptyHint.Text = Strings.PanelEmpty;
        Items.Width = columns * ItemWidth;
        Scroller.MaxHeight = MaxVisibleRows * ItemHeight;
        ShowFolder(folder);

        SourceInitialized += OnSourceInitialized;
        Closing += (_, _) => _closing = true;
        Deactivated += (_, _) => Dismiss();
    }

    public event EventHandler<FolderItem>? ItemInvoked;

    public WidgetFolder Folder { get; private set; }

    /// <summary>Shows <paramref name="folder"/>'s current items; also used to refresh an open panel.</summary>
    public void ShowFolder(WidgetFolder folder)
    {
        Folder = folder;
        Heading.Text = folder.Name;
        var items = folder.Items.Select(i => new PanelItem(i)).ToList();
        Items.ItemsSource = items;
        EmptyHint.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var item in items)
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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Dismiss();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

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
        Frame.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Width = Frame.DesiredSize.Width;
        Height = Frame.DesiredSize.Height;
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

            if (!Backdrop.TryApplyAcrylic(window, _theme.IsDark))
            {
                Frame.Background = new SolidColorBrush(_theme.Background);
            }
        }

        // Before the first show, so the panel never flashes elsewhere.
        Reposition(window);
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

    private void OnItemClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is PanelItem item)
        {
            ItemInvoked?.Invoke(this, item.Item);
        }
    }
}
