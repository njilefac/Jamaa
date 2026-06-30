using System;
using System.Threading.Tasks;

namespace Jamaa.Desktop.Services.Updater;

public interface IApplicationUpdateService
{
    void ConfigureCloseApplication(Action closeApplication);

    void ScheduleAutomaticCheckAfterLogin();

    Task CheckForUpdatesAtUserRequestAsync();

    void Stop();
}
