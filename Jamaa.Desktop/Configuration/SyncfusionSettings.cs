namespace Jamaa.Desktop.Configuration;

public sealed record SyncfusionSettings
{
    public const string SectionName = "Syncfusion";

    public string LicenseKey { get; init; } = string.Empty;
}
