using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using Libota.Desktop.ViewModels.Events;
using Libota.Desktop.ViewModels.Finances;
using Libota.Desktop.ViewModels.Members;
using Libota.Desktop.ViewModels.Navigation;

namespace Libota.Desktop.ViewModels.Shared;

[UsedImplicitly]
public partial class DashboardViewModel: ObservableValidator
{
    private readonly IServiceProvider _serviceProvider;

    public DashboardViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        MenuItems =
        [
            new NavigationItemViewModel(typeof(MemberManagementScreenViewModel),"Members", "Icons.Members"),
            new NavigationItemViewModel(typeof(EventManagementViewModel),"Events", "Icons.Calendar"),
            new NavigationItemViewModel(typeof(FinanceManagementViewModel),"Finances", "Icons.Finances")
        ];
        
        SelectedItem = MenuItems.First(x => x.ViewModelType == typeof(MemberManagementScreenViewModel));
    }

    partial void OnSelectedItemChanged(NavigationItemViewModel value)
    {
        var vm = (ObservableObject)_serviceProvider.GetService(value.ViewModelType)!;
        CurrentPage = vm;
    }
    
    [ObservableProperty] private IEnumerable<NavigationItemViewModel> _menuItems;
    [ObservableProperty] private NavigationItemViewModel _selectedItem;
    [ObservableProperty] private ObservableObject? _currentPage;
}