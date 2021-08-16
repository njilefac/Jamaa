using ReactiveUI;

namespace Club.Station.Desktop.ViewModels
{
    public class UserManagementViewModel: ReactiveObject, IRoutableViewModel
    {
        public UserManagementViewModel(IScreen screen)
        {
            HostScreen = screen;
        }
        
        public string UrlPathSegment => "club.station.usermanagement"; 
        public IScreen HostScreen { get; }
    }
}