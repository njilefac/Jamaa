using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Models;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

/// <summary>
/// Integration: Hosts the Accounting Configuration sub-module and manages breadcrumb navigation within the configuration hierarchy.
/// </summary>
public partial class AccountingConfigurationViewModel : ObservableObject, IApplicationModule, INavigationHost
{
    private readonly IRouteResolver _routeResolver;
    private readonly IRelayCommand _openFiscalCalendarAndPeriodsCommand;
    private readonly IRelayCommand _openChartOfAccountsCommand;
    private readonly IRelayCommand _openTaxGroupsAndAuthoritiesCommand;
    private readonly IRelayCommand _openAutomationRulesCommand;
    private readonly IRelayCommand _openUserRolesAndApprovalsCommand;
    private readonly IRelayCommand _goToConfigurationFromBreadcrumbCommand;

    public AccountingConfigurationViewModel(IRouteResolver routeResolver)
    {
        _routeResolver = routeResolver;
        _openFiscalCalendarAndPeriodsCommand = new RelayCommand(OpenFiscalCalendarAndPeriods);
        _openChartOfAccountsCommand = new RelayCommand(OpenChartOfAccounts);
        _openTaxGroupsAndAuthoritiesCommand = new RelayCommand(OpenTaxGroupsAndAuthorities);
        _openAutomationRulesCommand = new RelayCommand(OpenAutomationRules);
        _openUserRolesAndApprovalsCommand = new RelayCommand(OpenUserRolesAndApprovals);
        _goToConfigurationFromBreadcrumbCommand = new RelayCommand(GoToConfigurationFromBreadcrumb);
        ConfigurationCards = CreateConfigurationCards();
        ShowConfigurationCards();
    }

    public Guid Id => Guid.Parse("f6a7b8c9-d1e2-4f3a-b4c5-d6e7f8a9b0c1");
    public string Title => "Accounting Configuration";
    public object? HeaderContent => null;

    public IReadOnlyList<AccountingConfigurationCardViewModel> ConfigurationCards { get; }
    public ObservableCollection<BreadcrumbItemModel> Breadcrumbs { get; } = [];

    [ObservableProperty] private object? _activeContent;
    [ObservableProperty] private bool _isConfigurationCardsVisible;
    [ObservableProperty] private bool _isSubPageVisible;

    private IReadOnlyList<AccountingConfigurationCardViewModel> CreateConfigurationCards() =>
    [
        new("Fiscal Calendar & Periods", _openFiscalCalendarAndPeriodsCommand),
        new("COA Structure & Mappings", _openChartOfAccountsCommand),
        new("Tax Groups & Authorities", _openTaxGroupsAndAuthoritiesCommand),
        new("Automation Rules", _openAutomationRulesCommand),
        new("User Roles & Approvals", _openUserRolesAndApprovalsCommand)
    ];

    private void OpenFiscalCalendarAndPeriods()
    {
        RequestNavigation(Routes.FiscalCalendarAndPeriods);
    }

    private void OpenChartOfAccounts()
    {
        RequestNavigation(Routes.ChartOfAccounts);
    }

    private void OpenTaxGroupsAndAuthorities()
    {
        RequestNavigation(Routes.TaxGroupsAndAuthorities);
    }

    private void OpenAutomationRules()
    {
        RequestNavigation(Routes.AutomationRules);
    }

    private void OpenUserRolesAndApprovals()
    {
        RequestNavigation(Routes.UserRolesAndApprovals);
    }

    private void RequestNavigation(string route)
    {
        WeakReferenceMessenger.Default.Send(new ModuleSelected(route));
    }

    private void UpdateBreadcrumbs(string route)
    {
        Breadcrumbs.Clear();
        
        if (route == Routes.AccountingConfiguration)
        {
            // No breadcrumbs shown on main configuration page
            return;
        }
        
        // For sub-pages: Configuration > [Sub-page title]
        Breadcrumbs.Add(new BreadcrumbItemModel("Configuration", Routes.AccountingConfiguration, true, _goToConfigurationFromBreadcrumbCommand));
        Breadcrumbs.Add(new BreadcrumbItemModel(ResolveBreadcrumbTitle(route), route));
    }

    private string ResolveBreadcrumbTitle(string route)
    {
        return route switch
        {
            Routes.FiscalCalendarAndPeriods => "Fiscal Calendar & Periods",
            Routes.ChartOfAccounts => "COA Structure & Mappings",
            Routes.TaxGroupsAndAuthorities => "Tax Groups & Authorities",
            Routes.AutomationRules => "Automation Rules",
            Routes.UserRolesAndApprovals => "User Roles & Approvals",
            _ => ResolveFromViewModel(route)
        };
    }

    private string ResolveFromViewModel(string route)
    {
        var resolvedContent = _routeResolver.Resolve(route);
        return (resolvedContent as IRouteableViewModel)?.Title ?? "Details";
    }

    private void ShowConfigurationCards()
    {
        ActiveContent = null;
        IsConfigurationCardsVisible = true;
        IsSubPageVisible = false;
        UpdateBreadcrumbs(Routes.AccountingConfiguration);
    }

    private void GoToConfigurationFromBreadcrumb()
    {
        ShowConfigurationCards();
    }

    public void NavigateTo<TViewModel>(object? parameter = null)
    {
    }

    public void NavigateTo(string route, object? parameter = null)
    {
        if (route == Routes.AccountingConfiguration)
        {
            ShowConfigurationCards();
            return;
        }

        var resolvedContent = _routeResolver.Resolve(route, parameter) ?? throw new InvalidOperationException();

        ActiveContent = resolvedContent;
        IsConfigurationCardsVisible = false;
        IsSubPageVisible = true;
        UpdateBreadcrumbs(route);
    }

    public bool CanGoBack()
    {
        return IsSubPageVisible;
    }

    public void GoBack()
    {
        ShowConfigurationCards();
    }

    public bool CanGoForward()
    {
        return false;
    }

    public void GoForward()
    {
    }
}
