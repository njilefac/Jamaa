using Domain.Services;
using ReactiveUI;

namespace Club.Station.Desktop.ViewModels
{
    public class FinanceManagementViewModel : ReactiveObject, IRoutableViewModel
    {
        private readonly IUserSessionService _userSessionService;
        
        public string? UrlPathSegment => "club.station.members";
        public IScreen HostScreen { get; }

        public FinanceManagementViewModel(IUserSessionService userSessionService, IScreen screen)
        {
            _userSessionService = userSessionService;
            HostScreen = screen;
        }
    }
}