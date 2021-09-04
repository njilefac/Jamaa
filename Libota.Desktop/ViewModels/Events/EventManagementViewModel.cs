using Libota.Application.Users.Services;
using ReactiveUI;

namespace Libota.Desktop.ViewModels.Events
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