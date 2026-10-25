using System.Windows;
using DeskGrid.Resources;

namespace DeskGrid.Updates;

internal enum UpdateChoice
{
    /// <summary>Not now: ask again the next time DeskGrid starts.</summary>
    Later,

    /// <summary>The new version was downloaded and is ready to be put in place.</summary>
    Update,

    /// <summary>Stay on this version and stop asking about the new one.</summary>
    Keep,
}

/// <summary>Tells the user a newer release exists and lets them update, keep this version or decide later.</summary>
internal partial class UpdateDialog : Window
{
    private readonly Func<IProgress<double>, Task<string?>> _download;
    private bool _downloading;

    /// <param name="download">Fetches the new version; returns why it failed, or <see langword="null"/>.</param>
    public UpdateDialog(ReleaseInfo release, Func<IProgress<double>, Task<string?>> download)
    {
        InitializeComponent();
        _download = download;
        Title = Strings.UpdateTitle;
        Message.Text = string.Format(Strings.UpdateAvailable, release.Version.ToString(3), UpdateChecker.CurrentVersion.ToString(3));
        Hint.Text = Strings.UpdateHint;
        UpdateButton.Content = Strings.UpdateButtonUpdate;
        KeepButton.Content = Strings.UpdateButtonKeep;
        LaterButton.Content = Strings.UpdateButtonLater;
        ProgressText.Text = Strings.UpdateDownloading;
    }

    public UpdateChoice Choice { get; private set; } = UpdateChoice.Later;

    // The download can't be cancelled, and its result has nowhere to go once the dialog is gone.
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = _downloading;
        base.OnClosing(e);
    }

    private async void OnUpdate(object sender, RoutedEventArgs e)
    {
        Error.Visibility = Visibility.Collapsed;
        _downloading = true;
        UpdateButton.IsEnabled = KeepButton.IsEnabled = LaterButton.IsEnabled = false;
        ProgressArea.Visibility = Visibility.Visible;
        Progress.Value = 0;

        string? problem;
        try
        {
            problem = await _download(new Progress<double>(value => Progress.Value = value));
        }
        finally
        {
            _downloading = false;
        }

        if (problem is null)
        {
            Choice = UpdateChoice.Update;
            DialogResult = true;
            return;
        }

        ProgressArea.Visibility = Visibility.Collapsed;
        Error.Text = string.Format(Strings.UpdateFailed, problem);
        Error.Visibility = Visibility.Visible;
        UpdateButton.IsEnabled = KeepButton.IsEnabled = LaterButton.IsEnabled = true;
    }

    private void OnKeep(object sender, RoutedEventArgs e)
    {
        Choice = UpdateChoice.Keep;
        DialogResult = true;
    }
}
