using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Libota.Desktop.Navigation.Interfaces;
using Libota.Desktop.Navigation.Models;
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
    [ObservableProperty] private IApplicationModule? _activeModule;

    public DashboardViewModel(IRouteResolver routeResolver)
    {
        _routeResolver = routeResolver;

        WeakReferenceMessenger.Default.RegisterAll(this);

        MenuItems = GetNavigationItems();
        SelectedItem = MenuItems.FirstOrDefault();
    }

    private static IEnumerable<NavigationItemModel> GetNavigationItems()
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
        ActiveModule = GetModuleForRoute(message.Route);
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
    
    private IApplicationModule GetModuleForRoute(string route)
    {
        if (_moduleCache.TryGetValue(route, out var cachedModule))
        {
            return cachedModule ?? throw new InvalidOperationException();
        }

        var module = _routeResolver.Resolve(route) as IApplicationModule ?? throw new InvalidOperationException($"Route '{route}' did not resolve to a valid view model.");
        _moduleCache[route] = module;
        return module;
    }
    
    private readonly Dictionary<string, IApplicationModule?> _moduleCache = new();
}