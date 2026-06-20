using System.Collections.Generic;
using NetSparkleUpdater;
using NetSparkleUpdater.Interfaces;
using NetSparkleUpdater.UI.Avalonia;

namespace Jamaa.Desktop.Services.Updater;

public class JamaaUiFactory : UIFactory
{
    public override IUpdateAvailable CreateUpdateAvailableWindow(List<AppCastItem> updates, ISignatureVerifier? signatureVerifier, string currentVersion, string appName, bool isUpdateAlreadyDownloaded)
    {
        return new Jamaa.Desktop.Services.Updater.Views.UpdateAvailableWindow(updates, isUpdateAlreadyDownloaded, currentVersion);
    }

    public override IDownloadProgress CreateProgressWindow(string title, string afterDownloadButtonTitle)
    {
        return new Jamaa.Desktop.Services.Updater.Views.DownloadProgressWindow(title, afterDownloadButtonTitle);
    }
}
