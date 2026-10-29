using System.IO;
using DeskGrid.Folders;

namespace DeskGrid.Tests;

public sealed class DesktopFilesTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void ByDisplayName_LeavesOutNamesSeveralFilesShare()
    {
        var chrome = _temp.CreateFile(@"user\Chrome.lnk");
        var notesText = _temp.CreateFile(@"user\Notes.txt");
        var notesLink = _temp.CreateFile(@"user\Notes.lnk");
        var publicChrome = _temp.CreateFile(@"public\chrome.lnk");
        var slack = _temp.CreateFile(@"public\Slack.lnk");

        // Extensions of shortcuts are never shown; "Notes.txt" has its extension hidden, as by default for known types.
        var names = DesktopFiles.ByDisplayName(
            [chrome, notesText, notesLink, publicChrome, slack],
            path => Path.GetFileNameWithoutExtension(path));

        Assert.Equal([slack], names.Values);
        Assert.Equal(slack, names["SLACK"]);
    }

    [Fact]
    public void ByDisplayName_SkipsMissingDesktops_AndHiddenFiles()
    {
        var shortcut = _temp.CreateFile(@"user\Chrome.lnk");
        _temp.CreateFile(@"user\desktop.ini", FileAttributes.Hidden | FileAttributes.System);

        var names = DesktopFiles.ByDisplayName([_temp.Combine("user"), _temp.Combine("nowhere")]);

        Assert.Equal(shortcut, names.Single().Value);
    }
}
