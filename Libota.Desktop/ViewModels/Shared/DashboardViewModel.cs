using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Libota.Desktop.Navigation;
using Libota.Desktop.ViewModels.Navigation;

namespace Libota.Desktop.ViewModels.Shared;

[UsedImplicitly]
public partial class DashboardViewModel : ObservableValidator, IRecipient<ModuleSelected>, IDisposable
{
    private readonly IRouteResolver _routeResolver;
    [ObservableProperty] private IEnumerable<NavigationItemViewModel> _menuItems;
    [ObservableProperty] private NavigationItemViewModel? _selectedItem;
    [ObservableProperty] private object? _activeContent;

    public DashboardViewModel(IRouteResolver routeResolver)
    {
        _routeResolver = routeResolver;

        WeakReferenceMessenger.Default.Register(this);

        MenuItems = GetNavigationMenuItems();
        SelectedItem = MenuItems.FirstOrDefault();
    }

    private static IEnumerable<NavigationItemViewModel> GetNavigationMenuItems()
    {
        //TODO: Load dynamically based on user permissions
        //TODO: this should be done by a factory or service
        return
        [
            new NavigationItemViewModel(Routes.MembersOverview, "Members", "Icons.Members"),
            new NavigationItemViewModel(Routes.EventsOverview, "Events", "Icons.Calendar"),
            new NavigationItemViewModel(Routes.FinancesOverview, "Finances", "Icons.Finances")
        ];
    }

    partial void OnSelectedItemChanged(NavigationItemViewModel? value)
    {
        if (value == null)
        {
            return;
        }
        WeakReferenceMessenger.Default.Send(new ModuleSelected(value.TargetRoute));
    }

    public void Receive(ModuleSelected message)
    {
        ActiveContent = GetViewModelForRoute(message.Path);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
    
    private object? GetViewModelForRoute(string path)
    {
        if (_viewModelCache.TryGetValue(path, out var cachedVm))
        {
            return cachedVm;
        }

        var vm = _routeResolver.Resolve(path);
        _viewModelCache[path] = vm;
        return vm;
    }
    
    private readonly Dictionary<string, object?> _viewModelCache = new();
}