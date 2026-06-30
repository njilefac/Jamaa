using System;
using System.Collections.Generic;
using Avalonia.Threading;
using Jamaa.Desktop.Services.Notifications;
using NetSparkleUpdater;
using NetSparkleUpdater.Interfaces;
using NetSparkleUpdater.UI.Avalonia;
using JamaaNotificationType = Jamaa.Desktop.Services.Notifications.NotificationType;

namespace Jamaa.Desktop.Services.Updater;

public class JamaaUiFactory(INotificationService notificationService) : UIFactory
{
    private static readonly TimeSpan ToastExpiration = TimeSpan.FromSeconds(12);

    public override IUpdateAvailable CreateUpdateAvailableWindow(List<AppCastItem> updates, ISignatureVerifier? signatureVerifier, string currentVersion, string appName, bool isUpdateAlreadyDownloaded)
    {
        return new Jamaa.Desktop.Services.Updater.Views.UpdateAvailableWindow(updates, isUpdateAlreadyDownloaded, currentVersion);
    }

    public override IDownloadProgress CreateProgressWindow(string title, string afterDownloadButtonTitle)
    {
        return new Jamaa.Desktop.Services.Updater.Views.DownloadProgressWindow(title, afterDownloadButtonTitle);
    }

    public override bool CanShowToastMessages()
    {
        return true;
    }

    public override ICheckingForUpdates ShowCheckingForUpdates()
    {
        return new SilentCheckingForUpdates();
    }

    public override void ShowVersionIsUpToDate()
    {
    }

    public override void ShowVersionIsSkippedByUserRequest()
    {
    }

    public override void ShowCannotDownloadAppcast(string? appcastUrl)
    {
    }

    public override void ShowDownloadErrorMessage(string message, string? appcastUrl)
    {
    }

    public override void ShowToast(Action clickHandler)
    {
        Dispatcher.UIThread.Post(() =>
            notificationService.Show(
                "Update available",
                "A new version of Jamaa is ready to install.",
                JamaaNotificationType.Information,
                ToastExpiration,
                () => Dispatcher.UIThread.Post(clickHandler)));
    }

    private sealed class SilentCheckingForUpdates : ICheckingForUpdates
    {
        public event EventHandler? UpdatesUIClosing;

        public void Show()
        {
        }

        public void Close()
        {
            UpdatesUIClosing?.Invoke(this, EventArgs.Empty);
        }
    }
}
