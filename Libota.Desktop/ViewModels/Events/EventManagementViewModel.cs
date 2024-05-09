using Libota.Application.Users.Services;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;

namespace Libota.Desktop.ViewModels.Events
{
    public class EventManagementViewModel : ReactiveObject, IRoutableViewModel
    {
        private readonly IUserSessionService _userSessionService;

        public string? UrlPathSegment => "events"; 
        public IScreen HostScreen { get; }

        public EventManagementViewModel(MainWindowViewModel screen, IUserSessionService userSessionService)
        {
            HostScreen = screen;
            _userSessionService = userSessionService;
        }
    }
}