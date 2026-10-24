using DeskFolders.Config;
using DeskFolders.Desktop;
using DeskFolders.Folders;
using DeskFolders.Icons;

namespace DeskFolders.Panel;

/// <summary>Opens at most one <see cref="PopupPanel"/> at a time and keeps it in step with its folder.</summary>
internal sealed class PanelController(ConfigStore configStore, IconService icons)
{
    private const int MinColumns = 1;
    private const int MaxColumns = 8;

    private PopupPanel? _panel;

    /// <summary>
    /// Opens <paramref name="folder"/>'s panel next to <paramref name="anchor"/>, or closes it if it is
    /// already open. Widgets never take activation, so an open panel is still open when its widget is clicked.
    /// </summary>
    public void Toggle(WidgetFolder folder, ScreenRect anchor)
    {
        var wasOpen = _panel is { } open && Same(open.Folder, folder);
        _panel?.Dismiss();
        if (wasOpen)
        {
            return;
        }

        var config = configStore.Config;
        var theme = PanelTheme.Resolve(config.PanelBackground, PanelTheme.SystemUsesDarkTheme, Backdrop.IsAcrylicSupported);
        var panel = new PopupPanel(folder, anchor, Math.Clamp(config.PanelColumns, MinColumns, MaxColumns), theme, icons);
        panel.ItemInvoked += (_, item) =>
        {
            ShellItem.Open(item.FullPath);
            panel.Dismiss();
        };
        panel.Closed += (_, _) =>
        {
            if (_panel == panel)
            {
                _panel = null;
            }
        };

        _panel = panel;
        panel.Show();
        panel.Activate();
    }

    /// <summary>Refreshes the open panel after a rescan, or closes it if its folder is gone.</summary>
    public void Refresh(IReadOnlyList<WidgetFolder> widgets)
    {
        if (_panel is not { } panel)
        {
            return;
        }

        if (widgets.FirstOrDefault(w => Same(w, panel.Folder)) is { } folder)
        {
            panel.ShowFolder(folder);
        }
        else
        {
            panel.Dismiss();
        }
    }

    public void Close() => _panel?.Dismiss();

    private static bool Same(WidgetFolder a, WidgetFolder b) => string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
}
