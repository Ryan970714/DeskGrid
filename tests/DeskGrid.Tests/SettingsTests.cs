using System.IO;
using DeskGrid.Folders;
using DeskGrid.Resources;
using DeskGrid.Tray;
using Microsoft.Win32;

namespace DeskGrid.Tests;

public sealed class WidgetFolderNameTests : IDisposable
{
    private readonly TempDirectory _root = new();

    public void Dispose() => _root.Dispose();

    [Fact]
    public void Suggest_NumbersTheDefaultNameWhenTaken()
    {
        Assert.Equal(Strings.NewFolderDefaultName, WidgetFolderName.Suggest(_root.Path));

        Directory.CreateDirectory(_root.Combine(Strings.NewFolderDefaultName));
        Assert.Equal($"{Strings.NewFolderDefaultName} (2)", WidgetFolderName.Suggest(_root.Path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a/b")]
    [InlineData("what?")]
    [InlineData("trailing.")]
    [InlineData("CON")]
    [InlineData("com1.txt")]
    public void Validate_RejectsNamesWindowsWontCreate(string name) =>
        Assert.Equal(Strings.NewFolderInvalidName, WidgetFolderName.Validate(_root.Path, name));

    [Fact]
    public void Validate_RejectsExistingNames_AcceptsNewOnes()
    {
        Directory.CreateDirectory(_root.Combine("Games"));

        Assert.Equal(Strings.NewFolderExists, WidgetFolderName.Validate(_root.Path, "games"));
        Assert.Null(WidgetFolderName.Validate(_root.Path, "  Work  "));
    }
}

public sealed class AutoStartTests : IDisposable
{
    private const string Command = "\"C:\\Apps\\DeskGrid.exe\"";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    // A throwaway stand-in for HKEY_CURRENT_USER.
    private readonly string _rootPath = $@"Software\DeskGrid.Tests\{Guid.NewGuid():N}";
    private readonly RegistryKey _root;

    public AutoStartTests() => _root = Registry.CurrentUser.CreateSubKey(_rootPath);

    public void Dispose()
    {
        _root.Dispose();
        Registry.CurrentUser.DeleteSubKeyTree(_rootPath, throwOnMissingSubKey: false);
    }

    [Fact]
    public void SetEnabled_WritesAndRemovesTheRunValue()
    {
        var autoStart = new AutoStart(_root, Command);
        Assert.False(autoStart.IsEnabled);

        autoStart.SetEnabled(true);
        Assert.True(autoStart.IsEnabled);
        Assert.Equal(Command, RunValue());

        autoStart.SetEnabled(false);
        Assert.False(autoStart.IsEnabled);
        Assert.Null(RunValue());
    }

    [Fact]
    public void TaskManagerSwitch_IsHonored_AndClearedWhenReEnabled()
    {
        var autoStart = new AutoStart(_root, Command);
        autoStart.SetEnabled(true);
        using (var approved = _root.CreateSubKey(ApprovedKey))
        {
            approved.SetValue(AutoStart.ValueName, new byte[] { 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);
        }

        Assert.False(autoStart.IsEnabled);

        autoStart.SetEnabled(true);
        Assert.True(autoStart.IsEnabled);
    }

    [Fact]
    public void UpdatePath_FixesAStalePath_OnlyWhenEnabled()
    {
        new AutoStart(_root, "\"C:\\Old\\DeskGrid.exe\"").SetEnabled(true);

        new AutoStart(_root, Command).UpdatePath();
        Assert.Equal(Command, RunValue());

        new AutoStart(_root, Command).SetEnabled(false);
        new AutoStart(_root, "\"C:\\Other\\DeskGrid.exe\"").UpdatePath();
        Assert.Null(RunValue());
    }

    private string? RunValue()
    {
        using var run = _root.OpenSubKey(RunKey);
        return run?.GetValue(AutoStart.ValueName) as string;
    }
}
