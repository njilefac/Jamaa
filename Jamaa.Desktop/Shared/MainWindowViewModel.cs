using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Models;
using Jamaa.Desktop.Services.Navigation.Values;
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
        if (value == null || _isSynchronizingSelection)
        {
            return;
        }

        WeakReferenceMessenger.Default.Send(new ModuleSelected(value.TargetRoute));
    }

    public void Receive(ModuleSelected message)
    {
        var moduleRoute = ResolveModuleRoute(message.Route);
        var module = GetModuleForRoute(moduleRoute);
        var shouldDelegateToHost = message.Route != moduleRoute;
        if (shouldDelegateToHost && module is INavigationHost navigationHost)
        {
            navigationHost.NavigateTo(message.Route);
        }

        ActiveModule = module;
        SynchronizeSelectedItem(message.Route);
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

    private void SynchronizeSelectedItem(string route)
    {
        var navigationItem = FindBestNavigationItem(route);
        if (navigationItem == null || ReferenceEquals(SelectedItem, navigationItem))
        {
            return;
        }

        _isSynchronizingSelection = true;
        try
        {
            SelectedItem = navigationItem;
        }
        finally
        {
            _isSynchronizingSelection = false;
        }
    }

    private NavigationItemModel? FindBestNavigationItem(string route)
    {
        var allItems = FlattenNavigationItems(MenuItems).ToList();

        if (route.StartsWith(Routes.AccountingOverview, StringComparison.Ordinal))
        {
            return allItems.FirstOrDefault(x => x.TargetRoute == Routes.AccountingDashboard);
        }

        var exactMatch = allItems.FirstOrDefault(x => x.TargetRoute == route);
        if (exactMatch != null)
        {
            return exactMatch;
        }

        return allItems
            .Where(x => route.StartsWith(x.TargetRoute + "/", StringComparison.Ordinal))
            .OrderByDescending(x => x.TargetRoute.Length)
            .FirstOrDefault();
    }

    private static IEnumerable<NavigationItemModel> FlattenNavigationItems(IEnumerable<NavigationItemModel> items)
    {
        foreach (var item in items)
        {
            yield return item;

            if (item.SubItems == null)
            {
                continue;
            }

            foreach (var subItem in FlattenNavigationItems(item.SubItems))
            {
                yield return subItem;
            }
        }
    }

    private static string ResolveModuleRoute(string route)
    {
        if (route.StartsWith(Routes.AccountingConfiguration + "/", StringComparison.Ordinal))
        {
            return Routes.AccountingDashboard;
        }

        return route switch
        {
            Routes.AccountingOverview => Routes.AccountingDashboard,
            Routes.AccountingDashboard => Routes.AccountingDashboard,
            Routes.AccountingTransactions => Routes.AccountingDashboard,
            Routes.BankReconciliation => Routes.AccountingDashboard,
            Routes.AccountingReports => Routes.AccountingDashboard,
            Routes.ChartOfAccounts => Routes.AccountingDashboard,
            Routes.AccountingConfiguration => Routes.AccountingDashboard,
            _ => route
        };
    }
    
    private readonly Dictionary<string, IApplicationModule?> _moduleCache = new();
    private bool _isSynchronizingSelection;
}