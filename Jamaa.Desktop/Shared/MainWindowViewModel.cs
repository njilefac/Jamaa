using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Jamaa.Application.Accounting;
using Jamaa.Application.Users.Services;
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
    private readonly Dictionary<string, IApplicationModule?> _moduleCache = new();
    private readonly IAccountingFacade _accountingFacade;
    private readonly IRouteResolver _routeResolver;
    private readonly IUserSessionService _userSessionService;
    [ObservableProperty] private IApplicationModule? _activeModule;
    private bool _isSynchronizingSelection;
    [ObservableProperty] private IEnumerable<NavigationItemModel> _menuItems;
    [ObservableProperty] private NavigationItemModel? _selectedItem;
    private long _navigationVersion;

    public MainWindowViewModel(
        IRouteResolver routeResolver,
        INavigationItemsProvider navigationItemsProvider,
        IAccountingFacade accountingFacade,
        IUserSessionService userSessionService)
    {
        _routeResolver = routeResolver;
        _accountingFacade = accountingFacade;
        _userSessionService = userSessionService;

        WeakReferenceMessenger.Default.RegisterAll(this);

        MenuItems = navigationItemsProvider.GetNavigationItems();
        SelectedItem = MenuItems.FirstOrDefault();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    public void Receive(ModuleSelected message)
    {
        var navigationVersion = Interlocked.Increment(ref _navigationVersion);

        if (IsAccountingModuleRootRoute(message.Route))
        {
            SynchronizeSelectedItem(message.Route);
            _ = HandleAccountingModuleSelectionAsync(navigationVersion);
            return;
        }

        var moduleRoute = ResolveModuleRoute(message.Route);
        var module = GetModuleForRoute(moduleRoute);
        var shouldDelegateToHost = message.Route != moduleRoute;
        if (shouldDelegateToHost && module is INavigationHost navigationHost)
            navigationHost.NavigateTo(message.Route, message.Parameter);

        ActiveModule = module;
        SynchronizeSelectedItem(message.Route);
    }

    partial void OnSelectedItemChanged(NavigationItemModel? value)
    {
        if (value == null || _isSynchronizingSelection) return;

        WeakReferenceMessenger.Default.Send(new ModuleSelected(value.TargetRoute));
    }

    public void NavigateToSettings()
    {
        WeakReferenceMessenger.Default.Send(new ModuleSelected(Routes.Settings));
    }

    private IApplicationModule GetModuleForRoute(string route)
    {
        if (_moduleCache.TryGetValue(route, out var cachedModule))
            return cachedModule ?? throw new InvalidOperationException();

        var module = _routeResolver.Resolve(route) as IApplicationModule ??
                     throw new InvalidOperationException($"Route '{route}' did not resolve to a valid view model.");
        _moduleCache[route] = module;
        return module;
    }

    private void SynchronizeSelectedItem(string route)
    {
        if (IsSettingsRoute(route))
        {
            if (SelectedItem == null) return;

            _isSynchronizingSelection = true;
            try
            {
                SelectedItem = null;
            }
            finally
            {
                _isSynchronizingSelection = false;
            }

            return;
        }

        var navigationItem = FindBestNavigationItem(route);
        if (navigationItem == null) return;

        if (ReferenceEquals(SelectedItem, navigationItem)) return;

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

        if (IsAccountingModuleRootRoute(route))
            return allItems.FirstOrDefault(x => x.TargetRoute == Routes.AccountingDashboard);

        if (route.StartsWith(Routes.AccountingOverview, StringComparison.Ordinal))
            return allItems.FirstOrDefault(x => x.TargetRoute == Routes.AccountingDashboard);

        var exactMatch = allItems.FirstOrDefault(x => x.TargetRoute == route);
        if (exactMatch != null) return exactMatch;

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

            if (item.SubItems == null) continue;

            foreach (var subItem in FlattenNavigationItems(item.SubItems)) yield return subItem;
        }
    }

    private static string ResolveModuleRoute(string route)
    {
        if (IsAccountingConfigurationRoute(route)) return Routes.Settings;

        return route switch
        {
            Routes.AccountingOverview => Routes.AccountingDashboard,
            Routes.AccountingDashboard => Routes.AccountingDashboard,
            Routes.AccountingTransactions => Routes.AccountingDashboard,
            Routes.BankReconciliation => Routes.AccountingDashboard,
            Routes.AccountingReports => Routes.AccountingDashboard,
            Routes.Settings => Routes.Settings,
            _ => route
        };
    }

    private async Task HandleAccountingModuleSelectionAsync(long navigationVersion)
    {
        try
        {
            var route = await ResolveAccountingModuleRouteAsync();
            if (navigationVersion != Interlocked.Read(ref _navigationVersion)) return;

            ActiveModule = GetModuleForRoute(route);
        }
        catch
        {
            if (navigationVersion != Interlocked.Read(ref _navigationVersion)) return;

            ActiveModule = GetModuleForRoute(Routes.AccountingDashboard);
        }
    }

    private async Task<string> ResolveAccountingModuleRouteAsync()
    {
        var organisationId = _userSessionService.CurrentUserSession?.Organisation?.Id;
        if (string.IsNullOrWhiteSpace(organisationId)) return Routes.AccountingDashboard;

        var isSetupComplete = await _accountingFacade.IsAccountingSetupComplete(organisationId);
        return isSetupComplete ? Routes.AccountingDashboard : Routes.AccountingSetupWizard;
    }

    private static bool IsAccountingModuleRootRoute(string route)
    {
        return route is Routes.AccountingOverview or Routes.AccountingDashboard;
    }

    private static bool IsAccountingConfigurationRoute(string route)
    {
        return route == Routes.AccountingConfiguration
               || route.StartsWith(Routes.AccountingConfiguration + "/", StringComparison.Ordinal);
    }

    private static bool IsSettingsRoute(string route)
    {
        return route == Routes.Settings
               || route.StartsWith(Routes.Settings + "/", StringComparison.Ordinal)
               || IsAccountingConfigurationRoute(route);
    }
}