using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Libota.Desktop.Navigation;
using Libota.Desktop.ViewModels.Events;
using Libota.Desktop.ViewModels.Finances;
using Libota.Desktop.ViewModels.Members;
using Libota.Desktop.ViewModels.Navigation;

namespace Libota.Desktop.ViewModels.Shared;

public partial class DashboardViewModel: ObservableValidator
{
    private readonly INavigationService _navigationService;

    public DashboardViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        MenuItems =
        [
            new NavigationItemViewModel("Home", "Icons.Home", typeof(DashboardViewModel)),
            new NavigationItemViewModel("Members", "Icons.Members", typeof(MembersManagementScreenViewModel)),
            new NavigationItemViewModel("Events", "Icons.Calendar", typeof(EventManagementViewModel)),
            new NavigationItemViewModel("Finances", "Icons.Finances", typeof(FinanceManagementViewModel))
        ];
    }
    
    [RelayCommand]
    private void NavigateTo(Type viewModelType)
    {
        _navigationService.NavigateTo(viewModelType);
    }

    [ObservableProperty] private IEnumerable<NavigationItemViewModel> _menuItems;
}