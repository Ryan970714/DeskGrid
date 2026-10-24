using System.IO;
using System.Text.Json.Serialization;
using DeskFolders.Desktop;

namespace DeskFolders.Config;

/// <summary>
/// Everything persisted in config.json. Folder contents are never stored here: the folders on disk
/// are the source of truth, this only adds what the file system can't hold.
/// </summary>
internal sealed class AppConfig
{
    public const int DefaultPanelColumns = 3;

    public static string DefaultRootPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "DeskFolders");

    public string RootPath { get; set; } = DefaultRootPath;

    public int PanelColumns { get; set; } = DefaultPanelColumns;

    public DropMode DropMode { get; set; } = DropMode.Move;

    public PanelBackground PanelBackground { get; set; } = new();

    /// <summary>Keyed by folder name under <see cref="RootPath"/>; case-insensitive like the file system.</summary>
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<string, WidgetConfig> Widgets { get; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class WidgetConfig
{
    /// <summary>Top-left corner in physical screen pixels; <see langword="null"/> until the widget is placed.</summary>
    public ScreenPoint? Position { get; set; }

    /// <summary>Item file names in display order.</summary>
    public List<string> ItemOrder { get; set; } = [];
}

/// <summary>The popup panel's background.</summary>
internal sealed class PanelBackground
{
    public PanelBackgroundStyle Style { get; set; } = PanelBackgroundStyle.System;

    /// <summary>"#RRGGBB", used with <see cref="PanelBackgroundStyle.Custom"/>.</summary>
    public string CustomColor { get; set; } = "#2B2B2B";

    /// <summary>0 to 1, used with <see cref="PanelBackgroundStyle.Custom"/>.</summary>
    public double CustomOpacity { get; set; } = 0.85;
}

internal enum PanelBackgroundStyle
{
    /// <summary>System acrylic in the system's light/dark theme (Windows 11); opaque theme color elsewhere.</summary>
    System,
    Dark,
    Light,
    Custom,
}

/// <summary>What dropping a desktop shortcut onto a widget does. Dropped .exe files always get a new shortcut.</summary>
internal enum DropMode
{
    Move,
    Copy,
}
