using System.IO;
using DeskFolders.Folders;

namespace DeskFolders.Tests;

public sealed class FolderScannerTests : IDisposable
{
    private readonly TempDirectory _root = new();
    private readonly DispatcherThread _ui = new();

    public void Dispose()
    {
        _ui.Dispose();
        _root.Dispose();
    }

    [Fact]
    public async Task Scan_ListsVisibleFilesOfEachSubfolderInNaturalOrder()
    {
        _root.CreateFile(@"Work\notes.txt");
        _root.CreateFile(@"Games\item10.lnk");
        _root.CreateFile(@"Games\item2.url");
        _root.CreateFile(@"Games\hidden.lnk", FileAttributes.Hidden);
        _root.CreateFile(@"Games\Nested\deep.exe");
        _root.CreateFile("loose-file.txt");

        var widgets = await _ui.InvokeAsync(() =>
        {
            using var scanner = new FolderScanner(_root.Path);
            return scanner.Scan();
        });

        Assert.NotNull(widgets);
        Assert.Equal(["Games", "Work"], widgets.Select(w => w.Name));
        Assert.Equal(["item2.url", "item10.lnk"], widgets[0].Items.Select(i => i.Name));
        Assert.Equal(["notes.txt"], widgets[1].Items.Select(i => i.Name));
    }

    [Fact]
    public async Task Scan_ReturnsNullWhenRootIsGone()
    {
        var result = await _ui.InvokeAsync(() =>
        {
            using var scanner = new FolderScanner(_root.Path);
            Directory.Delete(_root.Path);
            return scanner.Scan();
        });

        Assert.Null(result);
    }

    [Fact]
    public void Constructor_RequiresSynchronizationContext() =>
        Assert.Throws<InvalidOperationException>(() => new FolderScanner(_root.Path));
}
