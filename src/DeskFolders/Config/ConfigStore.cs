using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeskFolders.Diagnostics;

namespace DeskFolders.Config;

/// <summary>Owns the loaded <see cref="AppConfig"/> and writes it back to disk.</summary>
internal sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter<DropMode>(JsonNamingPolicy.CamelCase) },
    };

    private ConfigStore(string filePath, AppConfig config)
    {
        FilePath = filePath;
        Config = config;
    }

    public static string DefaultFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskFolders", "config.json");

    public string FilePath { get; }

    public AppConfig Config { get; }

    /// <summary>
    /// Loads the config, or defaults if the file doesn't exist. An unreadable file is moved aside rather
    /// than overwritten, so hand edits are never silently lost.
    /// </summary>
    public static ConfigStore Open(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new ConfigStore(filePath, new AppConfig());
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            return new ConfigStore(filePath, JsonSerializer.Deserialize<AppConfig>(stream, JsonOptions) ?? new AppConfig());
        }
        catch (JsonException exception)
        {
            var backupPath = $"{filePath}.{DateTime.Now:yyyyMMdd-HHmmss}.bad";
            File.Move(filePath, backupPath);
            Log.Error($"Invalid config moved to {backupPath}; using defaults", exception);
            return new ConfigStore(filePath, new AppConfig());
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        // Write-then-rename: a crash mid-write never leaves a truncated config behind.
        var tempPath = FilePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(Config, JsonOptions));
        File.Move(tempPath, FilePath, overwrite: true);
    }
}
