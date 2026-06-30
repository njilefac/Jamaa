using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Jamaa.Desktop.Configuration;
using Jamaa.Desktop.Services.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using JamaaNotificationType = Jamaa.Desktop.Services.Notifications.NotificationType;

namespace Jamaa.Desktop.Services.Updater;

public class ApplicationUpdateService(
    IOptions<UpdaterSettings> options,
    JamaaUiFactory uiFactory,
    INotificationService notificationService,
    ILogger<ApplicationUpdateService> logger)
    : IApplicationUpdateService
{
    private static readonly TimeSpan AutomaticCheckDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan StatusNotificationExpiration = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan UpdateAvailableNotificationExpiration = TimeSpan.FromSeconds(30);
    private static readonly string LastCheckStatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Jamaa",
        "update-check-state.txt");

    private readonly SemaphoreSlim _checkLock = new(1, 1);
    private bool _automaticCheckScheduledThisSession;
    private Action? _closeApplication;
    private CancellationTokenSource? _automaticCheckCancellation;
    private SparkleUpdater? _sparkle;

    public void ConfigureCloseApplication(Action closeApplication)
    {
        _closeApplication = closeApplication;
    }

    public void ScheduleAutomaticCheckAfterLogin()
    {
        if (_automaticCheckScheduledThisSession)
            return;

        _automaticCheckScheduledThisSession = true;

        if (!ShouldRunAutomaticCheckToday())
        {
            logger.LogInformation("Automatic update check skipped because Jamaa already checked for updates today.");
            return;
        }

        _automaticCheckCancellation?.Cancel();
        _automaticCheckCancellation?.Dispose();
        _automaticCheckCancellation = new CancellationTokenSource();
        _ = RunDelayedAutomaticCheckAsync(_automaticCheckCancellation.Token);
    }

    public async Task CheckForUpdatesAtUserRequestAsync()
    {
        if (!await _checkLock.WaitAsync(0))
        {
            ShowNotification("Update check already running", "Jamaa is already checking for updates.", JamaaNotificationType.Information);
            return;
        }

        try
        {
            var settings = options.Value;
            if (string.IsNullOrWhiteSpace(settings.UpdateUrl))
            {
                ShowNotification("Updates unavailable", "The update URL is not configured.", JamaaNotificationType.Warning);
                logger.LogWarning("Update URL is not configured.");
                return;
            }

            var updater = _sparkle ??= CreateUpdater(settings.UpdateUrl, null);
            WriteLastCheckDate(DateTimeOffset.Now);
            var updateInfo = await CheckForUpdatesQuietlyOnUiThread(updater);
            NotifyManualCheckResult(updateInfo, updater);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check for updates.");
            ShowNotification("Update check failed", "Jamaa could not check for updates.", JamaaNotificationType.Error);
        }
        finally
        {
            _checkLock.Release();
        }
    }

    public void Stop()
    {
        _automaticCheckCancellation?.Cancel();
        _automaticCheckCancellation?.Dispose();
        _automaticCheckCancellation = null;
        _sparkle?.StopLoop();
        _sparkle = null;
    }

    private SparkleUpdater CreateUpdater(string updateUrl, Action? closeApplication)
    {
        var updater = new SparkleUpdater(updateUrl, new DSAChecker(SecurityMode.Unsafe))
        {
            UIFactory = uiFactory,
            RelaunchAfterUpdate = true,
            UseNotificationToast = true
        };

        updater.AppCastHelper.AppCastFilter = new InstalledVersionAppCastFilter();

        var closeHandler = closeApplication ?? _closeApplication;
        if (closeHandler != null)
        {
            updater.CloseApplication += () => closeHandler();
        }

        return updater;
    }

    private async Task RunDelayedAutomaticCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AutomaticCheckDelay, cancellationToken);
            await CheckForUpdatesAutomaticallyAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Automatic update check was cancelled.");
        }
    }

    private async Task CheckForUpdatesAutomaticallyAsync(CancellationToken cancellationToken)
    {
        if (!ShouldRunAutomaticCheckToday())
        {
            logger.LogInformation("Automatic update check skipped because Jamaa already checked for updates today.");
            return;
        }

        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.UpdateUrl))
        {
            logger.LogWarning("Update URL is not configured.");
            return;
        }

        if (!await _checkLock.WaitAsync(0, cancellationToken))
        {
            logger.LogInformation("Automatic update check skipped because another update check is already running.");
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteLastCheckDate(DateTimeOffset.Now);
            var updater = _sparkle ??= CreateUpdater(settings.UpdateUrl, null);
            var updateInfo = await CheckForUpdatesQuietlyOnUiThread(updater);
            NotifyAutomaticCheckResult(updateInfo, updater);
            logger.LogInformation("Automatic update check completed.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check for updates automatically.");
        }
        finally
        {
            _checkLock.Release();
        }
    }

    private static async Task<UpdateInfo> CheckForUpdatesQuietlyOnUiThread(SparkleUpdater updater)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return await updater.CheckForUpdatesQuietly();
        }

        return await Dispatcher.UIThread.InvokeAsync(async () => await updater.CheckForUpdatesQuietly());
    }

    private static bool ShouldRunAutomaticCheckToday()
    {
        var lastCheckDate = ReadLastCheckDate();
        return lastCheckDate != DateOnly.FromDateTime(DateTimeOffset.Now.LocalDateTime);
    }

    private static DateOnly? ReadLastCheckDate()
    {
        if (!File.Exists(LastCheckStatePath))
            return null;

        var value = File.ReadAllText(LastCheckStatePath).Trim();
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static void WriteLastCheckDate(DateTimeOffset checkedAt)
    {
        var directory = Path.GetDirectoryName(LastCheckStatePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(LastCheckStatePath, checkedAt.LocalDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    private void NotifyManualCheckResult(UpdateInfo updateInfo, SparkleUpdater updater)
    {
        switch (updateInfo.Status)
        {
            case UpdateStatus.UpdateAvailable:
                ShowUpdateAvailableNotification(updateInfo, updater);
                break;
            case UpdateStatus.UpdateNotAvailable:
                ShowNotification(
                    "Jamaa is up to date",
                    $"Version {VersionService.GetDisplayVersion()} is the latest available.",
                    JamaaNotificationType.Success,
                    StatusNotificationExpiration);
                break;
            case UpdateStatus.UserSkipped:
                ShowNotification(
                    "Update skipped",
                    "The available update is currently marked as skipped.",
                    JamaaNotificationType.Information,
                    StatusNotificationExpiration);
                break;
            case UpdateStatus.CouldNotDetermine:
                ShowNotification(
                    "Update check failed",
                    "Jamaa could not determine whether an update is available.",
                    JamaaNotificationType.Warning,
                    StatusNotificationExpiration);
                break;
        }
    }

    private void NotifyAutomaticCheckResult(UpdateInfo updateInfo, SparkleUpdater updater)
    {
        if (updateInfo.Status == UpdateStatus.UpdateAvailable)
            ShowUpdateAvailableNotification(updateInfo, updater);
    }

    private void ShowUpdateAvailableNotification(UpdateInfo updateInfo, SparkleUpdater updater)
    {
        var message = GetUpdateAvailableMessage(updateInfo);
        ShowLinkNotification(
            "Update available",
            message,
            "Open update page",
            JamaaNotificationType.Information,
            UpdateAvailableNotificationExpiration,
            () => ShowUpdateWindow(updater, updateInfo));
    }

    private static string GetUpdateAvailableMessage(UpdateInfo updateInfo)
    {
        var version = updateInfo.Updates is { Count: > 0 }
            ? updateInfo.Updates[0].ShortVersion ?? updateInfo.Updates[0].Version
            : null;

        return string.IsNullOrWhiteSpace(version)
            ? "A newer version is available."
            : $"Version {version.TrimStart('v', 'V')} is available.";
    }

    private static void ShowUpdateWindow(SparkleUpdater updater, UpdateInfo updateInfo)
    {
        if (updateInfo.Updates is not { Count: > 0 })
            return;

        Dispatcher.UIThread.Post(() =>
        {
            var useNotificationToast = updater.UseNotificationToast;
            updater.UseNotificationToast = false;
            try
            {
                updater.ShowUpdateNeededUI(updateInfo.Updates);
            }
            finally
            {
                updater.UseNotificationToast = useNotificationToast;
            }
        });
    }

    private void ShowNotification(
        string title,
        string message,
        JamaaNotificationType type,
        TimeSpan? expiration = null,
        Action? onClick = null)
    {
        Dispatcher.UIThread.Post(() => notificationService.Show(title, message, type, expiration, onClick));
    }

    private void ShowLinkNotification(
        string title,
        string message,
        string linkText,
        JamaaNotificationType type,
        TimeSpan? expiration = null,
        Action? onLinkClick = null)
    {
        Dispatcher.UIThread.Post(() =>
            notificationService.ShowLink(title, message, linkText, type, expiration, onLinkClick));
    }
}
