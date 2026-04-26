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

/// <summary>
/// Integration: Hosts the Accounting workspace as a tabbed shell and synchronizes routes, tabs, and breadcrumbs.
/// </summary>
public partial class AccountingDashboardViewModel : ObservableObject, IApplicationModule, INavigationHost
{
    private readonly IRouteResolver _routeResolver;
    private readonly IRelayCommand _goToDashboardCommand;
    private readonly IRelayCommand _goToConfigurationCommand;
    private bool _isSynchronizingTabSelection;

    public AccountingDashboardViewModel(IRouteResolver routeResolver)
    {
        _routeResolver = routeResolver;
        _goToDashboardCommand = new RelayCommand(GoToDashboardFromBreadcrumb);
        _goToConfigurationCommand = new RelayCommand(GoToConfigurationFromBreadcrumb);
        ActionShortcuts = CreateActionShortcuts();
        MetricCards = CreateMetricCards();
        NavigateTo(Routes.AccountingDashboard);
    }

    public Guid Id => Guid.Parse("d1c8e5b0-9f3a-4c8b-9e2a-1f2b3c4d5e6f");
    public string Title => "Accounting";
    public object? HeaderContent => null;

    public IReadOnlyList<DashboardShortcutViewModel> ActionShortcuts { get; }
    public IReadOnlyList<DashboardMetricCardViewModel> MetricCards { get; }
    public ObservableCollection<BreadcrumbItemModel> Breadcrumbs { get; } = [];

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private object? _journalEntriesContent;
    [ObservableProperty] private object? _bankReconciliationContent;
    [ObservableProperty] private object? _reportsContent;
    [ObservableProperty] private AccountingConfigurationViewModel? _configurationContent;

    private IReadOnlyList<DashboardShortcutViewModel> CreateActionShortcuts() =>
    [
        new("New Journal Entry", "📝", new RelayCommand(NavigateToJournalEntries)),
        new("Total Expenses", "💸", new RelayCommand(NavigateToTotalExpenses)),
        new("Reconcile Bank", "🏦", new RelayCommand(NavigateToBankReconciliation)),
        new("View Reports", "📊", new RelayCommand(NavigateToReports)),
        new("Accounting Configuration", "🛠", new RelayCommand(NavigateToAccountingConfiguration))
    ];

    private IReadOnlyList<DashboardMetricCardViewModel> CreateMetricCards() =>
    [
        new("5", "Unreconciled Items", "Navigate to bank reconciliation to review pending items.", new RelayCommand(NavigateToBankReconciliation)),
        new("12", "Pending Approvals", "Review journal entries awaiting approval.", new RelayCommand(NavigateToJournalEntries)),
        new("$50,234.00", "Year-to-Date Revenue", "View detailed financial reports.", new RelayCommand(NavigateToReports)),
        new("3", "Chart Anomalies", "Review and resolve chart of accounts issues.", new RelayCommand(NavigateToChartOfAccounts))
    ];

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (_isSynchronizingTabSelection)
        {
            return;
        }

        NavigateTo(GetRouteForTabIndex(value));
    }

    private void NavigateToJournalEntries() =>
        NavigateTo(Routes.AccountingTransactions, JournalEntriesNavigationRequest.AllAccounts());

    private void NavigateToTotalExpenses() =>
        NavigateTo(Routes.AccountingTransactions, JournalEntriesNavigationRequest.OnlyExpenseAccounts());

    private void NavigateToBankReconciliation() => NavigateTo(Routes.BankReconciliation);

    private void NavigateToReports() => NavigateTo(Routes.AccountingReports);

    private void NavigateToChartOfAccounts() => NavigateTo(Routes.ChartOfAccounts);

    private void NavigateToAccountingConfiguration() => NavigateTo(Routes.AccountingConfiguration);

    private void GoToDashboardFromBreadcrumb() => NavigateTo(Routes.AccountingDashboard);

    private void GoToConfigurationFromBreadcrumb() => NavigateTo(Routes.AccountingConfiguration);

    private static void EnsureRouteIsNotNull(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            throw new InvalidOperationException("Route cannot be null or empty.");
        }
    }

    private static void EnsureResolvedContentIsNotNull(object? resolvedContent)
    {
        if (resolvedContent == null)
        {
            throw new InvalidOperationException("Could not resolve target route.");
        }
    }

    private static bool IsDashboardRoute(string route) => route is Routes.AccountingOverview or Routes.AccountingDashboard;

    private static bool IsNestedConfigurationRoute(string route) =>
        route.StartsWith(Routes.AccountingConfiguration + "/", StringComparison.Ordinal);

    private static string GetPrimaryRoute(string route)
    {
        if (IsDashboardRoute(route))
        {
            return Routes.AccountingDashboard;
        }

        if (IsNestedConfigurationRoute(route))
        {
            return Routes.AccountingConfiguration;
        }

        return route;
    }

    private int GetTabIndexForRoute(string route) => GetPrimaryRoute(route) switch
    {
        Routes.AccountingDashboard => 0,
        Routes.AccountingTransactions => 1,
        Routes.BankReconciliation => 2,
        Routes.AccountingReports => 3,
        Routes.AccountingConfiguration => 4,
        _ => throw new InvalidOperationException($"Route '{route}' is not handled by the Accounting tabs.")
    };

    private string GetRouteForTabIndex(int selectedTabIndex) => selectedTabIndex switch
    {
        0 => Routes.AccountingDashboard,
        1 => Routes.AccountingTransactions,
        2 => Routes.BankReconciliation,
        3 => Routes.AccountingReports,
        4 => Routes.AccountingConfiguration,
        _ => Routes.AccountingDashboard
    };

    private void SetSelectedTabIndex(int selectedTabIndex)
    {
        if (SelectedTabIndex == selectedTabIndex)
        {
            return;
        }

        _isSynchronizingTabSelection = true;
        try
        {
            SelectedTabIndex = selectedTabIndex;
        }
        finally
        {
            _isSynchronizingTabSelection = false;
        }
    }

    private object EnsureJournalEntriesContent() =>
        JournalEntriesContent ??= _routeResolver.Resolve(Routes.AccountingTransactions);

    private object EnsureBankReconciliationContent() =>
        BankReconciliationContent ??= _routeResolver.Resolve(Routes.BankReconciliation);

    private object EnsureReportsContent() =>
        ReportsContent ??= _routeResolver.Resolve(Routes.AccountingReports);

    private AccountingConfigurationViewModel EnsureConfigurationContent()
    {
        ConfigurationContent ??= _routeResolver.Resolve(Routes.AccountingConfiguration) as AccountingConfigurationViewModel
            ?? throw new InvalidOperationException("Could not resolve Accounting Configuration content.");

        return ConfigurationContent;
    }

    private string ResolveNestedRouteTitle(string route)
    {
        var resolvedContent = _routeResolver.Resolve(route);
        EnsureResolvedContentIsNotNull(resolvedContent);

        return (resolvedContent as IRouteableViewModel)?.Title
            ?? (resolvedContent as IApplicationModule)?.Title
            ?? "Details";
    }

    private string ResolveBreadcrumbLeafTitle(string route)
    {
        return GetPrimaryRoute(route) switch
        {
            Routes.AccountingDashboard => "Dashboard",
            Routes.AccountingTransactions => "Journal Entries",
            Routes.BankReconciliation => "Bank Reconciliation",
            Routes.AccountingReports => "Reports",
            Routes.AccountingConfiguration when IsNestedConfigurationRoute(route) => ResolveNestedRouteTitle(route),
            Routes.AccountingConfiguration => "Configuration",
            _ => "Accounting"
        };
    }

    private void UpdateBreadcrumbs(string route)
    {
        Breadcrumbs.Clear();

        var isDashboard = IsDashboardRoute(route);
        Breadcrumbs.Add(new BreadcrumbItemModel("Accounting", Routes.AccountingDashboard, !isDashboard, _goToDashboardCommand));

        if (isDashboard)
        {
            Breadcrumbs.Add(new BreadcrumbItemModel("Dashboard", Routes.AccountingDashboard));
            return;
        }

        var primaryRoute = GetPrimaryRoute(route);
        if (primaryRoute == Routes.AccountingConfiguration && IsNestedConfigurationRoute(route))
        {
            Breadcrumbs.Add(new BreadcrumbItemModel("Configuration", Routes.AccountingConfiguration, true, _goToConfigurationCommand));
            Breadcrumbs.Add(new BreadcrumbItemModel(ResolveBreadcrumbLeafTitle(route), route));
            return;
        }

        Breadcrumbs.Add(new BreadcrumbItemModel(ResolveBreadcrumbLeafTitle(route), primaryRoute));
    }

    public void NavigateTo<TViewModel>(object? parameter = null)
    {
    }

    public void NavigateTo(string route, object? parameter = null)
    {
        EnsureRouteIsNotNull(route);

        var primaryRoute = GetPrimaryRoute(route);
        SetSelectedTabIndex(GetTabIndexForRoute(primaryRoute));

        switch (primaryRoute)
        {
            case Routes.AccountingDashboard:
                break;
            case Routes.AccountingTransactions:
                var journalEntriesContent = EnsureJournalEntriesContent();
                EnsureResolvedContentIsNotNull(journalEntriesContent);
                if (journalEntriesContent is JournalEntriesViewModel journalEntriesViewModel)
                {
                    var filter = parameter as JournalEntriesNavigationRequest ?? JournalEntriesNavigationRequest.AllAccounts();
                    journalEntriesViewModel.ApplyNavigationFilter(filter);
                }
                break;
            case Routes.BankReconciliation:
                EnsureResolvedContentIsNotNull(EnsureBankReconciliationContent());
                break;
            case Routes.AccountingReports:
                EnsureResolvedContentIsNotNull(EnsureReportsContent());
                break;
            case Routes.AccountingConfiguration:
                var configurationContent = EnsureConfigurationContent();
                configurationContent.NavigateTo(route, parameter);
                break;
            default:
                throw new InvalidOperationException($"Route '{route}' is not handled by the Accounting tabs.");
        }

        UpdateBreadcrumbs(route);
    }

    public bool CanGoBack() => false;

    public void GoBack()
    {
    }

    public bool CanGoForward() => false;

    public void GoForward()
    {
    }
}