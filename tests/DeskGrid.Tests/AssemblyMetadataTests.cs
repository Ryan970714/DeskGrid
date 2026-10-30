using System.Reflection;
using DeskGrid.Updates;

namespace DeskGrid.Tests;

/// <summary>Code-signing services (SignPath Foundation) require a fixed product name and version.</summary>
public sealed class AssemblyMetadataTests
{
    private static readonly Assembly App = typeof(UpdateChecker).Assembly;

    [Fact]
    public void ProductName_IsDeskGrid() =>
        Assert.Equal("DeskGrid", App.GetCustomAttribute<AssemblyProductAttribute>()?.Product);

    [Fact]
    public void ProductVersion_HasNoSourceRevisionSuffix()
    {
        var version = App.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        Assert.NotNull(version);
        Assert.DoesNotContain('+', version);
    }
}
