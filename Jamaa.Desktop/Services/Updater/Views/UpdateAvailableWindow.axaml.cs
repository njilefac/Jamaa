using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.Interfaces;
using Huskui.Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
    private bool _isUpdateAlreadyDownloaded;

    public UpdateAvailableWindow()
    {
        InitializeComponent();
        SetupControls();
    }

    public UpdateAvailableWindow(List<AppCastItem> updates, bool isUpdateAlreadyDownloaded, string currentVersion)
    {
        InitializeComponent();
        SetupControls();
        Updates = updates;
        _isUpdateAlreadyDownloaded = isUpdateAlreadyDownloaded;

        if (_versionText != null)
        {
            _versionText.Text = $"Version {CurrentItem?.Version} is now available.";
        }

        if (_currentVersionText != null)
        {
            _currentVersionText.Text = FormatInstalledVersionLabel(currentVersion);
        }

        if (_releaseNotesTextBlock != null)
        {
            _releaseNotesTextBlock.Text = BuildReleaseNotes(updates);
        }

        if (_installButton != null)
        {
            _installButton.Click += async (s, e) => await HandleInstallRequested();
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

    public SparkleUpdater Updater { get; set; } = default!;
    public List<AppCastItem> Updates { get; set; } = [];
    public UpdateAvailableResult Result { get; private set; }
    public AppCastItem CurrentItem => Updates is { Count: > 0 } ? Updates[0] : null!;

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
        _isUpdateAlreadyDownloaded = isUpdateAlreadyDownloaded;
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

    internal static string FormatInstalledVersionLabel(string? currentVersion)
    {
        return FormatInstalledVersionLabel(currentVersion, VersionService.GetDisplayVersion());
    }

    internal static string FormatInstalledVersionLabel(string? currentVersion, string? fallbackVersion)
    {
        var normalizedCurrentVersion = NormalizeVersionForDisplay(currentVersion);
        if (string.IsNullOrWhiteSpace(normalizedCurrentVersion))
        {
            normalizedCurrentVersion = NormalizeVersionForDisplay(fallbackVersion);
        }

        if (string.IsNullOrWhiteSpace(normalizedCurrentVersion))
        {
            normalizedCurrentVersion = "0.0.0";
        }

        return $"Installed version: v{normalizedCurrentVersion}";
    }

    private static string NormalizeVersionForDisplay(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return string.Empty;
        }

        var normalizedVersion = version.Trim().TrimStart('v', 'V').Trim();
        var plusIndex = normalizedVersion.IndexOf('+');
        if (plusIndex > 0)
        {
            normalizedVersion = normalizedVersion.Substring(0, plusIndex);
        }

        return normalizedVersion;
    }

    private static string BuildReleaseNotes(IReadOnlyList<AppCastItem> updates)
    {
        if (updates.Count == 0)
        {
            return "No release notes available.";
        }

        var notes = new StringBuilder();

        foreach (var update in updates)
        {
            var description = update.Description;
            if (string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            if (notes.Length > 0)
            {
                notes.AppendLine();
                notes.AppendLine();
            }

            var version = string.IsNullOrWhiteSpace(update.ShortVersion)
                ? update.Version
                : update.ShortVersion;
            notes.AppendLine($"v{version}");
            notes.AppendLine(description.Trim());
        }

        if (notes.Length > 0)
        {
            return notes.ToString();
        }

        var latestReleaseNotesLink = updates.FirstOrDefault()?.ReleaseNotesLink;
        return !string.IsNullOrWhiteSpace(latestReleaseNotesLink)
            ? $"Release notes: {latestReleaseNotesLink}"
            : "No release notes available.";
    }

    private async Task HandleInstallRequested()
    {
        if (Updates.Count == 0)
        {
            Respond(UpdateAvailableResult.InstallUpdate);
            return;
        }

        var currentItem = CurrentItem;
        var downloadPathTask = Updater?.GetDownloadPathForAppCastItem(currentItem);
        var downloadPath = downloadPathTask == null ? null : await downloadPathTask;
        if (!ShouldUseDownloadedInstaller(_isUpdateAlreadyDownloaded, downloadPath))
        {
            Respond(UpdateAvailableResult.InstallUpdate);
            return;
        }

        await Updater!.InstallUpdate(currentItem, downloadPath);
        Close();
    }

    internal static bool ShouldUseDownloadedInstaller(bool isUpdateAlreadyDownloaded, string? downloadPath)
    {
        return isUpdateAlreadyDownloaded
               && !string.IsNullOrWhiteSpace(downloadPath)
               && File.Exists(downloadPath);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
