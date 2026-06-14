namespace Jamaa.Desktop.Configuration;

public sealed record UpdaterSettings
{
    public const string SectionName = "Updater";
    public string UpdateUrl { get; init; } = string.Empty;
    public int UpdateIntervalHours { get; init; } = 24;
}
