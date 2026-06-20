using Jamaa.Desktop.Services.Updater.Views;
using Shouldly;
using System.IO;
using Xunit;

namespace Tests.Services.Updater;

public class UpdateAvailableWindowTests
{
    [Theory]
    [InlineData("v1.2.3", "9.9.9", "Installed version: v1.2.3")]
    [InlineData("1.2.3-beta", "9.9.9", "Installed version: v1.2.3-beta")]
    [InlineData("v", "2.4.6-beta+sha", "Installed version: v2.4.6-beta")]
    [InlineData("v", "2.4.6", "Installed version: v2.4.6")]
    [InlineData("", "3.1.4", "Installed version: v3.1.4")]
    public void FormatInstalledVersionLabel_UsesCurrentVersionOrFallback(string currentVersion, string fallbackVersion, string expected)
    {
        var label = UpdateAvailableWindow.FormatInstalledVersionLabel(currentVersion, fallbackVersion);

        label.ShouldBe(expected);
    }

    [Fact]
    public void ShouldUseDownloadedInstaller_ReturnsTrue_WhenFlagSetAndFileExists()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            UpdateAvailableWindow.ShouldUseDownloadedInstaller(true, tempFile).ShouldBeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(false, "/tmp/any-file")]
    [InlineData(true, "")]
    [InlineData(true, "/tmp/non-existent-file-for-jamaa-tests")]
    public void ShouldUseDownloadedInstaller_ReturnsFalse_WhenPreconditionsNotMet(bool isUpdateAlreadyDownloaded, string path)
    {
        UpdateAvailableWindow.ShouldUseDownloadedInstaller(isUpdateAlreadyDownloaded, path).ShouldBeFalse();
    }
}
