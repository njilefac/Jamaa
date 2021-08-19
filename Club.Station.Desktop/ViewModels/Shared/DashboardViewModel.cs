using Domain.Services;
using ReactiveUI;

namespace Club.Station.Desktop.ViewModels.Shared
{
    public class DashboardViewModel: ReactiveObject, IRoutableViewModel
    {
        private readonly IUserSessionService _userSessionService;

        public DashboardViewModel(IUserSessionService userSessionService, IScreen hostScreen)
        {
            _userSessionService = userSessionService;
            HostScreen = hostScreen;
        }

        public string? UrlPathSegment => "Home";
        public IScreen HostScreen { get; }
    }
}