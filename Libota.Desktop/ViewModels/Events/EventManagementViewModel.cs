using CommunityToolkit.Mvvm.ComponentModel;
using Libota.Application.Users.Services;

namespace Libota.Desktop.ViewModels.Events
{
    public class EventManagementViewModel(IUserSessionService userSessionService) : ObservableObject
    {
        public string UrlPathSegment => "events"; 

        private readonly IUserSessionService _userSessionService;
    }
}