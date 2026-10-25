using System.IO;
using DeskGrid.Folders;
using DeskGrid.Resources;

namespace DeskGrid.Tests;

public sealed class RootFolderTests : IDisposable
{
    private static readonly string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Prepare_CreatesMissingFolder()
    {
        var path = _temp.Combine(@"a\b");

        Assert.Null(RootFolder.Prepare(path));
        Assert.True(Directory.Exists(path));
    }

    [Fact]
    public void Prepare_RejectsRelativePath() =>
        Assert.Equal(Strings.RootFolderNotAbsolute, RootFolder.Prepare(@"relative\path"));

    [Theory]
    [InlineData("")]
    [InlineData("DeskGrid")]
    public void Prepare_RejectsDesktopAndFoldersInsideIt(string subfolder) =>
        Assert.Equal(Strings.RootFolderOnDesktop, RootFolder.Prepare(Path.Combine(Desktop, subfolder)));

    [Fact]
    public void IsOnDesktop_IgnoresSiblingWithSamePrefix() =>
        Assert.False(RootFolder.IsOnDesktop(Desktop + "2"));
}
