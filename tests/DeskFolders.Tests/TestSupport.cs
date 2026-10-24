using System.IO;
using System.Runtime.CompilerServices;
using DeskFolders.Diagnostics;

namespace DeskFolders.Tests;

internal static class TestSetup
{
    [ModuleInitializer]
    internal static void RedirectLog() =>
        Log.FilePath = Path.Combine(Path.GetTempPath(), "DeskFolders.Tests", "test.log");
}

/// <summary>A unique, auto-deleted directory under %TEMP%.</summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() => Directory.CreateDirectory(Path);

    public string Path { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "DeskFolders.Tests", Guid.NewGuid().ToString("N"));

    public string Combine(string relativePath) => System.IO.Path.Combine(Path, relativePath);

    /// <summary>Creates an empty file (and its folders) at <paramref name="relativePath"/>.</summary>
    public string CreateFile(string relativePath, FileAttributes attributes = FileAttributes.Normal)
    {
        var path = Combine(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "");
        File.SetAttributes(path, attributes);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A watcher may still hold a handle; %TEMP% gets cleaned eventually.
        }
    }
}
