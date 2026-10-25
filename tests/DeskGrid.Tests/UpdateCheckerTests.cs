using DeskGrid.Updates;

namespace DeskGrid.Tests;

public sealed class UpdateCheckerTests
{
    private static readonly string Hash = new('a', 64);

    private static readonly string Release = $$"""
        {
          "tag_name": "v0.2.0",
          "html_url": "https://github.com/Ryan970714/DeskGrid/releases/tag/v0.2.0",
          "draft": false,
          "prerelease": false,
          "assets": [
            { "name": "notes.txt", "browser_download_url": "https://example.test/notes.txt" },
            { "name": "DeskGrid-0.2.0-win-x64.exe", "browser_download_url": "https://example.test/a.exe", "digest": "sha256:{{Hash}}" }
          ]
        }
        """;

    [Fact]
    public void ParseRelease_FindsTheVersionTheExeAndItsDigest()
    {
        var release = UpdateChecker.ParseRelease(Release);

        Assert.NotNull(release);
        Assert.Equal(new Version(0, 2, 0), release.Version);
        Assert.Equal("v0.2.0", release.Tag);
        Assert.Equal("https://example.test/a.exe", release.ExeUrl);
        Assert.Equal(Hash, release.Sha256);
    }

    [Fact]
    public void ParseRelease_WithoutAssets_HasNoDownload()
    {
        var release = UpdateChecker.ParseRelease("""{ "tag_name": "v1.0.0", "html_url": "u", "assets": [] }""");

        Assert.NotNull(release);
        Assert.Null(release.ExeUrl);
        Assert.Null(release.Sha256);
    }

    [Fact]
    public void ParseRelease_ExeWithoutDigest_HasNoChecksum()
    {
        var release = UpdateChecker.ParseRelease(
            """{ "tag_name": "v1.0.0", "html_url": "u", "assets": [ { "name": "DeskGrid-1.0.0-win-x64.exe", "browser_download_url": "x" } ] }""");

        Assert.NotNull(release);
        Assert.Equal("x", release.ExeUrl);
        Assert.Null(release.Sha256);
    }

    [Theory]
    [InlineData("""{ "tag_name": "v1.0.0", "html_url": "u", "draft": true }""")]
    [InlineData("""{ "tag_name": "v1.0.0", "html_url": "u", "prerelease": true }""")]
    [InlineData("""{ "tag_name": "nightly", "html_url": "u" }""")]
    public void ParseRelease_IgnoresDraftsPrereleasesAndOddTags(string json) =>
        Assert.Null(UpdateChecker.ParseRelease(json));

    [Theory]
    [InlineData("0.2.0", "0.1.0", true)]
    [InlineData("0.1.1", "0.1.0", true)]
    [InlineData("1.0.0", "0.9.9", true)]
    [InlineData("0.1.0", "0.1.0", false)]
    [InlineData("0.1.0", "0.1.0.0", false)]
    [InlineData("0.1.0", "0.2.0", false)]
    public void IsNewer_ComparesMajorMinorBuild(string latest, string current, bool expected) =>
        Assert.Equal(expected, UpdateChecker.IsNewer(Version.Parse(latest), Version.Parse(current)));

    [Fact]
    public void ParseDigest_AcceptsOnlySha256Hashes()
    {
        Assert.Equal(Hash, UpdateChecker.ParseDigest($"sha256:{Hash.ToUpperInvariant()}"));
        Assert.Null(UpdateChecker.ParseDigest("sha1:" + new string('a', 40)));
        Assert.Null(UpdateChecker.ParseDigest("sha256:short"));
        Assert.Null(UpdateChecker.ParseDigest(null));
    }
}
