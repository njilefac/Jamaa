using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Desktop.Navigation;

namespace Libota.Desktop.ViewModels.Users
{
    public class UserManagementViewModel(INavigationService navigationService): ObservableObject
    {
        public string UrlPathSegment => "users"; 
    }
}