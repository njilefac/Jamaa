using Domain.Services;
using ReactiveUI;

namespace Club.Station.Desktop.ViewModels
{
    public class EventManagementViewModel : ReactiveObject, IRoutableViewModel
    {
        private readonly IUserSessionService _userSessionService;

        public string? UrlPathSegment => "events"; 
        public IScreen HostScreen { get; }

        public EventManagementViewModel(IScreen screen, IUserSessionService userSessionService)
        {
            HostScreen = screen;
            _userSessionService = userSessionService;
        }
    }
}