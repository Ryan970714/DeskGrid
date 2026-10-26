using System.IO;
using DeskGrid.Shell;
using DeskGrid.Threading;

namespace DeskGrid.Tests;

public sealed class ShortcutFileTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly DispatcherThread _sta = new("Test STA");

    public void Dispose()
    {
        _sta.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public async Task Create_WritesTheAppUserModelId()
    {
        var target = _temp.CreateFile("tool.exe");
        var shortcut = _temp.Combine("tool.lnk");

        var id = await _sta.InvokeAsync(() =>
        {
            ShortcutFile.Create(shortcut, target, "--all", appUserModelId: "DeskGrid.Pin.Test");
            return ReadProperty(shortcut, "System.AppUserModel.ID");
        });

        Assert.Equal("DeskGrid.Pin.Test", id);
    }

    // Through the Shell's own property system, as the taskbar reads it.
    private static string? ReadProperty(string path, string property)
    {
        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application")!)!;
        var item = shell.NameSpace(Path.GetDirectoryName(path)).ParseName(Path.GetFileName(path));
        return item.ExtendedProperty(property) as string;
    }
}
