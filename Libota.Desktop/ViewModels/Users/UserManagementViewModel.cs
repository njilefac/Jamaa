using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;

namespace Libota.Desktop.ViewModels.Users
{
    public class UserManagementViewModel: ReactiveObject, IRoutableViewModel
    {
        public UserManagementViewModel(MainWindowViewModel screen)
        {
            HostScreen = screen;
        }
        
        public string UrlPathSegment => "users"; 
        public IScreen HostScreen { get; }
    }
}