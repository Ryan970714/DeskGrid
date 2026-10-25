using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using DeskGrid.Diagnostics;

namespace DeskGrid.Updates;

/// <summary>A published GitHub release of DeskGrid.</summary>
/// <param name="Tag">The release's tag, e.g. "v0.2.0".</param>
/// <param name="PageUrl">The release's web page.</param>
/// <param name="ExeUrl">The single exe attached to the release, or <see langword="null"/> if there is none.</param>
/// <param name="Sha256">The exe's SHA-256 (lower case hex) as GitHub computed it when the file was uploaded, or <see langword="null"/>.</param>
internal sealed record ReleaseInfo(Version Version, string Tag, string PageUrl, string? ExeUrl, string? Sha256);

/// <summary>Finds out whether GitHub has a newer release than the running version.</summary>
internal static class UpdateChecker
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/Ryan970714/DeskGrid/releases/latest";
    private const string ExeSuffix = "-win-x64.exe";

    public static Version CurrentVersion { get; } = Normalize(typeof(UpdateChecker).Assembly.GetName().Version ?? new Version(0, 0, 0));

    /// <summary>The newest release, or <see langword="null"/> if GitHub can't be reached or has none.</summary>
    public static async Task<ReleaseInfo?> GetLatestAsync(CancellationToken cancellation = default)
    {
        try
        {
            using var client = CreateClient(TimeSpan.FromSeconds(15));
            return ParseRelease(await client.GetStringAsync(LatestReleaseUrl, cancellation));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            Log.Error("Checking for updates failed", exception);
            return null;
        }
    }

    public static HttpClient CreateClient(TimeSpan timeout)
    {
        var client = new HttpClient { Timeout = timeout };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DeskGrid", CurrentVersion.ToString(3)));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    public static bool IsNewer(Version latest, Version current) => Normalize(latest) > Normalize(current);

    /// <summary>Reads the JSON of GitHub's "latest release"; <see langword="null"/> for a draft, a prerelease or an unreadable tag.</summary>
    internal static ReleaseInfo? ParseRelease(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (Flag(root, "draft") || Flag(root, "prerelease")
            || root.GetProperty("tag_name").GetString() is not { } tag
            || !Version.TryParse(tag.TrimStart('v', 'V'), out var version))
        {
            return null;
        }

        string? exeUrl = null, sha256 = null;
        if (root.TryGetProperty("assets", out var list))
        {
            foreach (var asset in list.EnumerateArray())
            {
                if (asset.GetProperty("name").GetString() is { } name && name.EndsWith(ExeSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    exeUrl = asset.GetProperty("browser_download_url").GetString();
                    sha256 = asset.TryGetProperty("digest", out var digest) ? ParseDigest(digest.GetString()) : null;
                    break;
                }
            }
        }

        return new ReleaseInfo(Normalize(version), tag, root.GetProperty("html_url").GetString() ?? "", exeUrl, sha256);
    }

    /// <summary>The hash of a GitHub asset digest ("sha256:&lt;hex&gt;"), lower case; <see langword="null"/> if it is anything else.</summary>
    internal static string? ParseDigest(string? digest)
    {
        const string Prefix = "sha256:";
        var hash = digest is not null && digest.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) ? digest[Prefix.Length..] : null;
        return hash is { Length: 64 } && hash.All(Uri.IsHexDigit) ? hash.ToLower(CultureInfo.InvariantCulture) : null;
    }

    // Major.minor.build: the fourth part of an assembly version is always 0 for us, and tags have three.
    private static Version Normalize(Version version) => new(version.Major, version.Minor, Math.Max(version.Build, 0));

    private static bool Flag(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
}
