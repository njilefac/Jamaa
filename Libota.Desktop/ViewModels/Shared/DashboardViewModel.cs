using Libota.Application.Users.Services;
using ReactiveUI;

namespace Libota.Desktop.ViewModels.Shared
{
    public class DashboardViewModel: ReactiveObject, IRoutableViewModel
    {
        private readonly IUserSessionService _userSessionService;

        public DashboardViewModel(IUserSessionService userSessionService, MainWindowViewModel hostScreen)
        {
            _userSessionService = userSessionService;
            HostScreen = hostScreen;
        }

        public string? UrlPathSegment => "Home";
        public IScreen HostScreen { get; }
    }
}