using System;
using System.Collections.Generic;
using Avalonia.Controls;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.Interfaces;
using Huskui.Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Jamaa.Desktop.Services;

namespace Jamaa.Desktop.Services.Updater.Views;

public partial class UpdateAvailableWindow : AppWindow, IUpdateAvailable
{
    private TextBlock? _versionText;
    private TextBlock? _currentVersionText;
    private TextBlock? _releaseNotesTextBlock;
    private Button? _installButton;
    private Button? _skipButton;
    private Button? _remindLaterButton;
    private ScrollViewer? _releaseNotesScrollViewer;
    private Border? _releaseNotesBorder;

    public UpdateAvailableWindow()
    {
        InitializeComponent();
        SetupControls();
    }

    public UpdateAvailableWindow(List<AppCastItem> updates, bool isUpdateAlreadyDownloaded, string releaseNotes)
    {
        InitializeComponent();
        SetupControls();
        Updates = updates;

        if (_versionText != null)
        {
            _versionText.Text = $"Version {CurrentItem?.Version} is now available.";
        }

        if (_currentVersionText != null)
        {
            _currentVersionText.Text = $"Installed version: {VersionService.GetVersion()}";
        }

        if (_releaseNotesTextBlock != null)
        {
            _releaseNotesTextBlock.Text = releaseNotes;
        }

        if (_installButton != null)
        {
            _installButton.Click += (s, e) => Respond(UpdateAvailableResult.InstallUpdate);
        }

        if (_skipButton != null)
        {
            _skipButton.Click += (s, e) => Respond(UpdateAvailableResult.SkipUpdate);
        }

        if (_remindLaterButton != null)
        {
            _remindLaterButton.Click += (s, e) => Respond(UpdateAvailableResult.RemindMeLater);
        }
    }

    private void SetupControls()
    {
        _versionText = this.FindControl<TextBlock>("VersionText");
        _currentVersionText = this.FindControl<TextBlock>("CurrentVersionText");
        _releaseNotesTextBlock = this.FindControl<TextBlock>("ReleaseNotes");
        _installButton = this.FindControl<Button>("InstallButton");
        _skipButton = this.FindControl<Button>("SkipButton");
        _remindLaterButton = this.FindControl<Button>("RemindLaterButton");
        
        // Find ReleaseNotes container if needed
        _releaseNotesScrollViewer = _releaseNotesTextBlock?.Parent as ScrollViewer;
        _releaseNotesBorder = _releaseNotesScrollViewer?.Parent as Border;
    }

    public SparkleUpdater Updater { get; set; }
    public List<AppCastItem> Updates { get; set; }
    public UpdateAvailableResult Result { get; private set; }
    public AppCastItem? CurrentItem => Updates is { Count: > 0 } ? Updates[0] : null;

    public event UserRespondedToUpdate? UserResponded;

    private void Respond(UpdateAvailableResult result)
    {
        Result = result;
        UserResponded?.Invoke(this, new NetSparkleUpdater.Events.UpdateResponseEventArgs(result, CurrentItem));
        Close();
    }

    public void HideUpdateAvailable()
    {
        Hide();
    }

    public void ShowUpdateAvailable(bool isUpdateAlreadyDownloaded)
    {
        Show();
    }

    public void BringToFront()
    {
        Activate();
    }

    public void HideReleaseNotes()
    {
        if (_releaseNotesBorder != null) _releaseNotesBorder.IsVisible = false;
    }

    public void HideRemindMeLaterButton()
    {
        if (_remindLaterButton != null) _remindLaterButton.IsVisible = false;
    }

    public void HideSkipButton()
    {
        if (_skipButton != null) _skipButton.IsVisible = false;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
