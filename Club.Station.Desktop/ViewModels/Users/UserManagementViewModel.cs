using ReactiveUI;

namespace Club.Station.Desktop.ViewModels.Users
{
    public class UserManagementViewModel: ReactiveObject, IRoutableViewModel
    {
        public UserManagementViewModel(IScreen screen)
        {
            HostScreen = screen;
        }
        
        public string UrlPathSegment => "users"; 
        public IScreen HostScreen { get; }
    }
}