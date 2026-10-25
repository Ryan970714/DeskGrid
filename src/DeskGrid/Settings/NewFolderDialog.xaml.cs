using System.Windows;
using DeskGrid.Folders;
using DeskGrid.Resources;

namespace DeskGrid.Settings;

/// <summary>Asks for the name of a new widget folder; the caller creates it.</summary>
internal partial class NewFolderDialog : Window
{
    private readonly string _rootPath;

    public NewFolderDialog(string rootPath)
    {
        InitializeComponent();
        Title = Strings.NewFolderTitle;
        Prompt.Text = Strings.NewFolderPrompt;
        OkButton.Content = Strings.ButtonOk;
        CancelButton.Content = Strings.ButtonCancel;

        _rootPath = rootPath;
        NameBox.Text = WidgetFolderName.Suggest(rootPath);
        Loaded += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
    }

    public string FolderName => NameBox.Text.Trim();

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (WidgetFolderName.Validate(_rootPath, FolderName) is { } problem)
        {
            Error.Text = problem;
            Error.Visibility = Visibility.Visible;
            return;
        }

        DialogResult = true;
    }
}
