using Libota.Application.Users.Services;
using ReactiveUI;

namespace Libota.Desktop.ViewModels.Groups
{
    public class GroupManagementViewModel: ReactiveObject, IRoutableViewModel
    {
        private readonly IUserSessionService _userSessionService;

        public GroupManagementViewModel(IUserSessionService userSessionService, IScreen hostScreen)
        {
            _userSessionService = userSessionService;
            HostScreen = hostScreen;
        }

        public string? UrlPathSegment => "groups";
        public IScreen HostScreen { get; }
    }
}