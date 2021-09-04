using Libota.Application.Users.Services;
using ReactiveUI;

namespace Libota.Desktop.ViewModels.Users
{
    public class AddUserScreenViewModel: ReactiveObject, IRoutableViewModel
    {
        public AddUserScreenViewModel(IUserSessionService userSessionService, IScreen hostScreen)
        {
            _userSessionService = userSessionService;
            HostScreen = hostScreen;
        }

        public string? UrlPathSegment => "add user";
        public IScreen HostScreen { get; }

        private readonly IUserSessionService _userSessionService;
    }
}