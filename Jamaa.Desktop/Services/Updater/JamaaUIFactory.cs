using System.Collections.Generic;
using NetSparkleUpdater;
using NetSparkleUpdater.Interfaces;
using NetSparkleUpdater.UI.Avalonia;

namespace Jamaa.Desktop.Services.Updater;

public class JamaaUIFactory : UIFactory
{
    public override IUpdateAvailable CreateUpdateAvailableWindow(List<AppCastItem> updates, ISignatureVerifier? signatureVerifier, string releaseNotes, string releaseNotesTemplate, bool isUpdateAlreadyDownloaded)
    {
        return new Jamaa.Desktop.Services.Updater.Views.UpdateAvailableWindow(updates, isUpdateAlreadyDownloaded, releaseNotes);
    }

    public override IDownloadProgress CreateProgressWindow(string title, string fileName)
    {
        return new Jamaa.Desktop.Services.Updater.Views.DownloadProgressWindow(title, fileName);
    }
}
