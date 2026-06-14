using System.Reflection;

namespace Jamaa.Desktop.Services;

public static class VersionService
{
    public static string GetVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return $"v{version?.ToString(3) ?? "1.0.0"}";
    }
}
