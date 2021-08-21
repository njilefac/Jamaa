using Domain.Services;
using ReactiveUI;

namespace Club.Station.Desktop.ViewModels.Finances
{
    public class FinanceManagementViewModel : ReactiveObject, IRoutableViewModel
    {
        private readonly IUserSessionService _userSessionService;
        
        public string? UrlPathSegment => "finance";
        public IScreen HostScreen { get; }

        public FinanceManagementViewModel(IUserSessionService userSessionService, IScreen screen)
        {
            _userSessionService = userSessionService;
            HostScreen = screen;
        }
    }
}