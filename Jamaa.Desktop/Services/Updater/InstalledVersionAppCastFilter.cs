using System.Collections.Generic;
using System.Linq;
using NetSparkleUpdater;
using NetSparkleUpdater.AppCastHandlers;
using NetSparkleUpdater.Interfaces;

namespace Jamaa.Desktop.Services.Updater;

public sealed class InstalledVersionAppCastFilter : IAppCastFilter
{
    public IEnumerable<AppCastItem> GetFilteredAppCastItems(SemVerLike installed, IEnumerable<AppCastItem> items)
    {
        var currentVersion = SemVerLike.Parse(VersionService.GetVersion());
        return items.Where(item => item.SemVerLikeVersion.CompareTo(currentVersion) > 0);
    }
}
