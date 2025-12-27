using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Libota.Desktop.Navigation.Interfaces;
using Libota.Desktop.Navigation.Models;
using Libota.Desktop.Navigation.Services;
using Libota.Desktop.Navigation.Values;

namespace Libota.Desktop.Shared;

[UsedImplicitly]
public partial class DashboardViewModel : ObservableValidator, 
    IRecipient<ModuleSelected>, 
    IDisposable
{
    private readonly IRouteResolver _routeResolver;
    [ObservableProperty] private IEnumerable<NavigationItemModel> _menuItems;
    [ObservableProperty] private NavigationItemModel? _selectedItem;
    [ObservableProperty] private object? _activeContent;

    public DashboardViewModel(IRouteResolver routeResolver)
    {
        _routeResolver = routeResolver;

        WeakReferenceMessenger.Default.RegisterAll(this);

        MenuItems = GetNavigationMenuItems();
        SelectedItem = MenuItems.FirstOrDefault();
    }

    private static IEnumerable<NavigationItemModel> GetNavigationMenuItems()
    {
        //TODO: Load dynamically based on user permissions
        //TODO: this should be done by a factory or service
        return
        [
            new NavigationItemModel(Routes.MembersOverview, "Members", "Icons.Members"),
            new NavigationItemModel(Routes.EventsOverview, "Events", "Icons.Calendar"),
            new NavigationItemModel(Routes.FinancesOverview, "Finances", "Icons.Finances")
        ];
    }

    partial void OnSelectedItemChanged(NavigationItemModel value)
    {
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