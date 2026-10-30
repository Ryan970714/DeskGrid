using System.Windows;
using System.Windows.Controls;
using DeskGrid.Resources;

namespace DeskGrid.Settings;

/// <summary>The change log of this version of DeskGrid and the ones before it.</summary>
internal partial class ChangeLogWindow : Window
{
    public ChangeLogWindow()
    {
        InitializeComponent();
        FluentWindow.UseOpaqueBackground(this);
        Title = Strings.ChangeLogTitle;
        CloseButton.Content = Strings.ButtonClose;

        if (ChangeLog.ReadEmbedded() is not { } markdown)
        {
            Entries.Children.Add(new TextBlock { Text = Strings.ChangeLogMissing, Opacity = 0.75, TextWrapping = TextWrapping.Wrap });
            return;
        }

        foreach (var line in ChangeLog.Parse(markdown))
        {
            Entries.Children.Add(Render(line));
        }
    }

    private static UIElement Render(ChangeLogLine line) => line.Kind switch
    {
        ChangeLogKind.Release => new TextBlock
        {
            Text = line.Text,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 16, 0, 4),
        },
        ChangeLogKind.Section => new TextBlock
        {
            Text = line.Text,
            FontWeight = FontWeights.SemiBold,
            Opacity = 0.75,
            Margin = new Thickness(0, 8, 0, 2),
        },
        _ => Bullet(line.Text),
    };

    private static Grid Bullet(string text)
    {
        var grid = new Grid { Margin = new Thickness(8, 2, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.Children.Add(new TextBlock { Text = "•" });

        var item = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(item, 1);
        grid.Children.Add(item);
        return grid;
    }
}
