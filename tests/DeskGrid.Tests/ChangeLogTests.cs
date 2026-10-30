using DeskGrid.Settings;

namespace DeskGrid.Tests;

public sealed class ChangeLogTests
{
    [Fact]
    public void Parse_ReadsReleasesSectionsAndItems()
    {
        const string markdown = """
            # Changelog

            ## 1.2.0

            ### Added

            - First thing.
            - Second thing.

            ### Fixed

            - A bug.

            ## 1.1.0

            ### Added

            - Older thing.
            """;

        var lines = ChangeLog.Parse(markdown);

        Assert.Equal(
            [
                (ChangeLogKind.Release, "1.2.0"),
                (ChangeLogKind.Section, "Added"),
                (ChangeLogKind.Item, "First thing."),
                (ChangeLogKind.Item, "Second thing."),
                (ChangeLogKind.Section, "Fixed"),
                (ChangeLogKind.Item, "A bug."),
                (ChangeLogKind.Release, "1.1.0"),
                (ChangeLogKind.Section, "Added"),
                (ChangeLogKind.Item, "Older thing."),
            ],
            lines.Select(l => (l.Kind, l.Text)));
    }

    [Fact]
    public void Parse_JoinsAnItemThatWrapsOverSeveralLines_AndDropsMarkdownMarks()
    {
        var lines = ChangeLog.Parse("## 1.0.0\r\n### Added\r\n- Starts with `--autostart` and\r\n  waits for **the desktop**.\r\n- Next.\r\n");

        Assert.Equal("Starts with --autostart and waits for the desktop.", lines[2].Text);
        Assert.Equal("Next.", lines[3].Text);
    }

    [Fact]
    public void Parse_IgnoresTheTitleAndTextOutsideItems()
    {
        var lines = ChangeLog.Parse("# Changelog\n\nSome intro.\n\n  indented, but not under an item\n");

        Assert.Empty(lines);
    }

    [Fact]
    public void ReadEmbedded_IsTheRepositorysChangeLog_WithTheCurrentVersionOrAnUnreleasedSectionOnTop()
    {
        var markdown = ChangeLog.ReadEmbedded();

        Assert.NotNull(markdown);
        var first = ChangeLog.Parse(markdown).First(l => l.Kind == ChangeLogKind.Release);
        var running = typeof(App).Assembly.GetName().Version!;
        Assert.True(
            first.Text == "Unreleased" || first.Text.StartsWith($"{running.Major}.{running.Minor}.{running.Build}", StringComparison.Ordinal),
            $"The newest entry is \"{first.Text}\" but this build is {running.ToString(3)}.");
    }
}
