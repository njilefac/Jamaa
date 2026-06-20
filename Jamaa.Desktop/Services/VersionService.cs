using System;
using System.Reflection;

namespace Jamaa.Desktop.Services;

public static class VersionService
{
    public static string GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return GetVersion(assembly);
    }

    public static string GetVersion(Assembly assembly)
    {
        // Use InformationalVersion if available (common for CI builds), otherwise fallback to Name.Version
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(infoVersion))
        {
            return NormalizeVersionString(infoVersion);
        }

        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "1.0.0";
    }

    public static Version GetComparableVersion()
    {
        return GetComparableVersion(GetVersion());
    }

    public static Version GetComparableVersion(string version)
    {
        var normalizedVersion = NormalizeVersionString(version);
        return Version.TryParse(normalizedVersion, out var parsedVersion)
            ? parsedVersion
            : new Version(0, 0, 0);
    }

    private static string NormalizeVersionString(string version)
    {
        var normalizedVersion = version;
        var plusIndex = normalizedVersion.IndexOf('+');
        if (plusIndex > 0)
        {
            normalizedVersion = normalizedVersion.Substring(0, plusIndex);
        }

        var dashIndex = normalizedVersion.IndexOf('-');
        if (dashIndex > 0)
        {
            normalizedVersion = normalizedVersion.Substring(0, dashIndex);
        }

        return normalizedVersion.Trim().TrimStart('v', 'V');
    }
}
