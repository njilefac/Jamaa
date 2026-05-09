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
///     Integration: Hosts the Accounting workspace as a tabbed shell and synchronizes routes, tabs, and breadcrumbs.
/// </summary>
public partial class AccountingDashboardViewModel : ObservableObject, IApplicationModule, INavigationHost
{
    private readonly IRelayCommand _goToDashboardCommand;
    private readonly IRouteResolver _routeResolver;
    [ObservableProperty] private object? _bankReconciliationContent;
    private bool _isSynchronizingTabSelection;
    [ObservableProperty] private object? _journalEntriesContent;
    [ObservableProperty] private object? _reportsContent;

    [ObservableProperty] private int _selectedTabIndex;

    public AccountingDashboardViewModel(IRouteResolver routeResolver)
    {
        _routeResolver = routeResolver;
        _goToDashboardCommand = new RelayCommand(GoToDashboardFromBreadcrumb);
        ActionShortcuts = CreateActionShortcuts();
        MetricCards = CreateMetricCards();
        NavigateTo(Routes.AccountingDashboard);
    }

    public IReadOnlyList<DashboardShortcutViewModel> ActionShortcuts { get; }
    public IReadOnlyList<DashboardMetricCardViewModel> MetricCards { get; }
    public ObservableCollection<BreadcrumbItemModel> Breadcrumbs { get; } = [];

    public Guid Id => Guid.Parse("d1c8e5b0-9f3a-4c8b-9e2a-1f2b3c4d5e6f");
    public string Title => "Accounting";
    public object? HeaderContent => null;

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
                    var filter = parameter as JournalEntriesNavigationRequest ??
                                 JournalEntriesNavigationRequest.AllAccounts();
                    journalEntriesViewModel.ApplyNavigationFilter(filter);
                }

                break;
            case Routes.BankReconciliation:
                EnsureResolvedContentIsNotNull(EnsureBankReconciliationContent());
                break;
            case Routes.AccountingReports:
                EnsureResolvedContentIsNotNull(EnsureReportsContent());
                break;
            default:
                throw new InvalidOperationException($"Route '{route}' is not handled by the Accounting tabs.");
        }

        UpdateBreadcrumbs(route);
    }

    public bool CanGoBack()
    {
        return false;
    }

    public void GoBack()
    {
    }

    public bool CanGoForward()
    {
        return false;
    }

    public void GoForward()
    {
    }

    private IReadOnlyList<DashboardShortcutViewModel> CreateActionShortcuts()
    {
        return
        [
            new("New Journal Entry", "📝", new RelayCommand(NavigateToJournalEntries)),
            new("Total Expenses", "💸", new RelayCommand(NavigateToTotalExpenses)),
            new("Reconcile Bank", "🏦", new RelayCommand(NavigateToBankReconciliation)),
            new("View Reports", "📊", new RelayCommand(NavigateToReports))
        ];
    }

    private IReadOnlyList<DashboardMetricCardViewModel> CreateMetricCards()
    {
        return
        [
            new("5", "Unreconciled Items", "Navigate to bank reconciliation to review pending items.",
                new RelayCommand(NavigateToBankReconciliation)),
            new("12", "Pending Approvals", "Review journal entries awaiting approval.",
                new RelayCommand(NavigateToJournalEntries)),
            new("$50,234.00", "Year-to-Date Revenue", "View detailed financial reports.",
                new RelayCommand(NavigateToReports)),
            new("3", "Chart Anomalies", "Review and resolve chart of accounts issues.",
                new RelayCommand(NavigateToChartOfAccounts))
        ];
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        if (_isSynchronizingTabSelection) return;

        NavigateTo(GetRouteForTabIndex(value));
    }

    private void NavigateToJournalEntries()
    {
        NavigateTo(Routes.AccountingTransactions, JournalEntriesNavigationRequest.AllAccounts());
    }

    private void NavigateToTotalExpenses()
    {
        NavigateTo(Routes.AccountingTransactions, JournalEntriesNavigationRequest.OnlyExpenseAccounts());
    }

    private void NavigateToBankReconciliation()
    {
        NavigateTo(Routes.BankReconciliation);
    }

    private void NavigateToReports()
    {
        NavigateTo(Routes.AccountingReports);
    }

    private static void NavigateToChartOfAccounts()
    {
        WeakReferenceMessenger.Default.Send(new ModuleSelected(Routes.ChartOfAccounts));
    }

    private void GoToDashboardFromBreadcrumb()
    {
        NavigateTo(Routes.AccountingDashboard);
    }

    private static void EnsureRouteIsNotNull(string route)
    {
        if (string.IsNullOrWhiteSpace(route)) throw new InvalidOperationException("Route cannot be null or empty.");
    }

    private static void EnsureResolvedContentIsNotNull(object? resolvedContent)
    {
        if (resolvedContent == null) throw new InvalidOperationException("Could not resolve target route.");
    }

    private static bool IsDashboardRoute(string route)
    {
        return route is Routes.AccountingOverview or Routes.AccountingDashboard;
    }

    private static string GetPrimaryRoute(string route)
    {
        if (IsDashboardRoute(route)) return Routes.AccountingDashboard;

        return route;
    }

    private int GetTabIndexForRoute(string route)
    {
        return GetPrimaryRoute(route) switch
        {
            Routes.AccountingDashboard => 0,
            Routes.AccountingTransactions => 1,
            Routes.BankReconciliation => 2,
            Routes.AccountingReports => 3,
            _ => throw new InvalidOperationException($"Route '{route}' is not handled by the Accounting tabs.")
        };
    }

    private string GetRouteForTabIndex(int selectedTabIndex)
    {
        return selectedTabIndex switch
        {
            0 => Routes.AccountingDashboard,
            1 => Routes.AccountingTransactions,
            2 => Routes.BankReconciliation,
            3 => Routes.AccountingReports,
            _ => Routes.AccountingDashboard
        };
    }

    private void SetSelectedTabIndex(int selectedTabIndex)
    {
        if (SelectedTabIndex == selectedTabIndex) return;

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

    private object EnsureJournalEntriesContent()
    {
        return JournalEntriesContent ??= _routeResolver.Resolve(Routes.AccountingTransactions);
    }

    private object EnsureBankReconciliationContent()
    {
        return BankReconciliationContent ??= _routeResolver.Resolve(Routes.BankReconciliation);
    }

    private object EnsureReportsContent()
    {
        return ReportsContent ??= _routeResolver.Resolve(Routes.AccountingReports);
    }

    private string ResolveBreadcrumbLeafTitle(string route)
    {
        return GetPrimaryRoute(route) switch
        {
            Routes.AccountingDashboard => "Dashboard",
            Routes.AccountingTransactions => "Journal Entries",
            Routes.BankReconciliation => "Bank Reconciliation",
            Routes.AccountingReports => "Reports",
            _ => "Accounting"
        };
    }

    private void UpdateBreadcrumbs(string route)
    {
        Breadcrumbs.Clear();

        var isDashboard = IsDashboardRoute(route);
        Breadcrumbs.Add(new BreadcrumbItemModel("Accounting", Routes.AccountingDashboard, !isDashboard,
            _goToDashboardCommand));

        if (isDashboard)
        {
            Breadcrumbs.Add(new BreadcrumbItemModel("Dashboard", Routes.AccountingDashboard));
            return;
        }

        var primaryRoute = GetPrimaryRoute(route);
        Breadcrumbs.Add(new BreadcrumbItemModel(ResolveBreadcrumbLeafTitle(route), primaryRoute));
    }
}