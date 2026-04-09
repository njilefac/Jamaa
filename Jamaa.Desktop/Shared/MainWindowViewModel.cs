using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Models;
using JetBrains.Annotations;

namespace Jamaa.Desktop.Shared;

[UsedImplicitly]
public partial class MainWindowViewModel : ObservableValidator, 
    IRecipient<ModuleSelected>, 
    IDisposable
{
    private readonly IRouteResolver _routeResolver;
    [ObservableProperty] private IEnumerable<NavigationItemModel> _menuItems;
    [ObservableProperty] private NavigationItemModel? _selectedItem;
    [ObservableProperty] private IApplicationModule? _activeModule;

    public MainWindowViewModel(IRouteResolver routeResolver, INavigationItemsProvider navigationItemsProvider)
    {
        _routeResolver = routeResolver;

        WeakReferenceMessenger.Default.RegisterAll(this);

        MenuItems = navigationItemsProvider.GetNavigationItems();
        SelectedItem = MenuItems.FirstOrDefault();
    }

    partial void OnSelectedItemChanged(NavigationItemModel? value)
    {
        if (value == null || (value.SubItems != null && value.SubItems.Any()))
        {
            return;
        }
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