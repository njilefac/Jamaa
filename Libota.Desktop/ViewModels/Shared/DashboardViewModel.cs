using System;
using System.Collections.Generic;
using Libota.Application.Users.Services;
using Libota.Desktop.ViewModels.Events;
using Libota.Desktop.ViewModels.Finances;
using Libota.Desktop.ViewModels.Members;
using Libota.Desktop.ViewModels.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace Libota.Desktop.ViewModels.Shared
{
    public class DashboardViewModel: ReactiveObject, IRoutableViewModel
    {
        public DashboardViewModel()
        {
            HostScreen = Locator.Current.GetService<MainWindowViewModel>() ?? throw new InvalidOperationException();
            _userSessionService = Locator.Current.GetService<IUserSessionService>() ?? throw new InvalidOperationException();
            MenuItems = new[]
            {
                new NavigationItemViewModel("Home", "Icons.Home", typeof(DashboardViewModel)),
                new NavigationItemViewModel("Members", "Icons.Members", typeof(MembersManagementScreenViewModel)),
                new NavigationItemViewModel("Events", "Icons.Calendar", typeof(EventManagementViewModel)),
                new NavigationItemViewModel("Finances", "Icons.Finances", typeof(FinanceManagementViewModel))
            };
        }

        [Reactive] public IEnumerable<NavigationItemViewModel> MenuItems { get; private set; }
        public string UrlPathSegment => "Home";
        public IScreen HostScreen { get; }

        private readonly IUserSessionService _userSessionService;
    }
}