using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Navigation.Models;
using Jamaa.Desktop.Services.Navigation.Values;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class AccountingConfigurationViewModel : ObservableObject, IApplicationModule, INavigationHost
{
    private readonly IRouteResolver _routeResolver;
    private readonly IRelayCommand _openFiscalCalendarAndPeriodsCommand;
    private readonly IRelayCommand _openChartOfAccountsCommand;
    private readonly IRelayCommand _openTaxGroupsAndAuthoritiesCommand;
    private readonly IRelayCommand _openAutomationRulesCommand;
    private readonly IRelayCommand _openUserRolesAndApprovalsCommand;

    public AccountingConfigurationViewModel(IRouteResolver routeResolver)
    {
        _routeResolver = routeResolver;
        _openFiscalCalendarAndPeriodsCommand = new RelayCommand(OpenFiscalCalendarAndPeriods);
        _openChartOfAccountsCommand = new RelayCommand(OpenChartOfAccounts);
        _openTaxGroupsAndAuthoritiesCommand = new RelayCommand(OpenTaxGroupsAndAuthorities);
        _openAutomationRulesCommand = new RelayCommand(OpenAutomationRules);
        _openUserRolesAndApprovalsCommand = new RelayCommand(OpenUserRolesAndApprovals);
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
        NavigateTo(Routes.FiscalCalendarAndPeriods);
    }

    private void OpenChartOfAccounts()
    {
        NavigateTo(Routes.ChartOfAccounts);
    }

    private void OpenTaxGroupsAndAuthorities()
    {
        NavigateTo(Routes.TaxGroupsAndAuthorities);
    }

    private void OpenAutomationRules()
    {
        NavigateTo(Routes.AutomationRules);
    }

    private void OpenUserRolesAndApprovals()
    {
        NavigateTo(Routes.UserRolesAndApprovals);
    }

    private void ShowConfigurationCards()
    {
        ActiveContent = null;
        IsConfigurationCardsVisible = true;
        IsSubPageVisible = false;

        Breadcrumbs.Clear();
        Breadcrumbs.Add(new BreadcrumbItemModel(Title, Routes.AccountingConfiguration));
    }

    public void NavigateTo<TViewModel>(object? parameter = null)
    {
    }

    public void NavigateTo(string route, object? parameter = null)
    {
        var resolvedContent = _routeResolver.Resolve(route, parameter) ?? throw new InvalidOperationException();
        var subPageTitle = (resolvedContent as IRouteableViewModel)?.Title ?? string.Empty;

        ActiveContent = resolvedContent;
        IsConfigurationCardsVisible = false;
        IsSubPageVisible = true;

        var backToRootCommand = new RelayCommand(ShowConfigurationCards);
        Breadcrumbs.Clear();
        Breadcrumbs.Add(new BreadcrumbItemModel(Title, Routes.AccountingConfiguration, true, backToRootCommand));
        Breadcrumbs.Add(new BreadcrumbItemModel(subPageTitle, route));
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
