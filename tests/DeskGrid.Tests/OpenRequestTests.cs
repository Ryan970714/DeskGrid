using DeskGrid.Taskbar;

namespace DeskGrid.Tests;

public sealed class OpenRequestTests
{
    [Fact]
    public void Arguments_RoundTripThroughParse() =>
        Assert.Equal("Dev tools", OpenRequest.Parse(["--open", "Dev tools"]));

    [Fact]
    public void Arguments_QuoteTheFolderName() =>
        Assert.Equal("--open \"Dev tools\"", OpenRequest.Arguments("Dev tools"));

    [Theory]
    [InlineData]
    [InlineData("--open")]
    [InlineData("--other", "Dev")]
    [InlineData("--open", "")]
    public void Parse_ReturnsNull_WhenNotAnOpenRequest(params string[] args) =>
        Assert.Null(OpenRequest.Parse(args));

    [Fact]
    public void Parse_IgnoresTheCaseOfTheSwitch() =>
        Assert.Equal("Dev", OpenRequest.Parse(["--OPEN", "Dev"]));
}
