using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Desktop.Accounting;
using Jamaa.Desktop.Events;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Settings;

/// <summary>
/// Integration: Hosts application settings sections and delegates accounting settings navigation to the accounting configuration module.
/// </summary>
public partial class SettingsViewModel : ObservableObject, IApplicationModule, INavigationHost
{
    private readonly IRouteResolver _routeResolver;

    public SettingsViewModel(IRouteResolver routeResolver)
    {
        _routeResolver = routeResolver;
        NavigateTo(Routes.Settings);
    }

    public Guid Id => Guid.Parse("3b67d1f8-8261-4b4d-bec5-3dd47e53fe7d");
    public string Title => "Settings";
    public object? HeaderContent => null;

    [ObservableProperty] private AccountingConfigurationViewModel? _accountingSettingsContent;
    [ObservableProperty] private EventsConfigurationViewModel? _eventsSettingsContent;

    public void NavigateTo<TViewModel>(object? parameter = null)
    {
    }

    // Integration: maps Settings routes to accounting settings routes and forwards navigation.
    public void NavigateTo(string route, object? parameter = null)
    {
        var accountingRoute = ResolveAccountingSettingsRoute(route);
        if (IsAccountingRoute(route))
        {
            EnsureAccountingSettingsContent().NavigateTo(accountingRoute, parameter);
        }
        else if (IsEventsRoute(route))
        {
            EnsureEventsSettingsContent().NavigateTo(route, parameter);
        }
    }

    public bool CanGoBack()
    {
        return EnsureAccountingSettingsContent().CanGoBack();
    }

    public void GoBack()
    {
        EnsureAccountingSettingsContent().GoBack();
    }

    public bool CanGoForward()
    {
        return false;
    }

    public void GoForward()
    {
    }

    // Operation: lazily resolves the accounting settings host view model once and reuses it.
    private AccountingConfigurationViewModel EnsureAccountingSettingsContent()
    {
        AccountingSettingsContent ??= _routeResolver.Resolve(Routes.AccountingConfiguration) as AccountingConfigurationViewModel
            ?? throw new InvalidOperationException("Could not resolve accounting settings content.");

        return AccountingSettingsContent;
    }

    // Operation: lazily resolves the events settings host view model once and reuses it.
    private EventsConfigurationViewModel EnsureEventsSettingsContent()
    {
        EventsSettingsContent ??= _routeResolver.Resolve(Routes.EventsConfiguration) as EventsConfigurationViewModel
            ?? throw new InvalidOperationException("Could not resolve events settings content.");

        return EventsSettingsContent;
    }

    // Operation: resolves the route that should be rendered in the accounting settings host.
    private static string ResolveAccountingSettingsRoute(string route)
    {
        if (route == Routes.Settings || route == Routes.OrganisationContactDetails)
        {
            return Routes.AccountingConfiguration;
        }

        return route;
    }

    private static bool IsAccountingRoute(string route)
    {
        return route == Routes.Settings
               || route == Routes.OrganisationContactDetails
               || route == Routes.AccountingConfiguration
               || route.StartsWith(Routes.AccountingConfiguration + "/", StringComparison.Ordinal);
    }

    private static bool IsEventsRoute(string route)
    {
        // Future events configuration routes would be checked here
        return false;
    }
}




