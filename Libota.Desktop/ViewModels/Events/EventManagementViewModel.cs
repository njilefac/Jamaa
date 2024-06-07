using System;
using Libota.Application.Users.Services;
using Libota.Desktop.ViewModels.Shared;
using ReactiveUI;
using Splat;

namespace Libota.Desktop.ViewModels.Events
{
    public class EventManagementViewModel : ReactiveObject, IRoutableViewModel
    {
        public EventManagementViewModel()
        {
            HostScreen = Locator.Current.GetService<MainWindowViewModel>() ?? throw new InvalidOperationException();;
            _userSessionService = Locator.Current.GetService<IUserSessionService>()!;
        }

        public string UrlPathSegment => "events"; 
        public IScreen HostScreen { get; }

        private readonly IUserSessionService _userSessionService;
    }
}