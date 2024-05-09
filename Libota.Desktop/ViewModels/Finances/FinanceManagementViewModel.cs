using Libota.Application.Users.Services;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;

namespace Libota.Desktop.ViewModels.Finances
{
    public class FinanceManagementViewModel : ReactiveObject, IRoutableViewModel
    {
        private readonly IUserSessionService _userSessionService;
        
        public string? UrlPathSegment => "finance";
        public IScreen HostScreen { get; }

        public FinanceManagementViewModel(IUserSessionService userSessionService, MainWindowViewModel screen)
        {
            _userSessionService = userSessionService;
            HostScreen = screen;
        }
    }
}