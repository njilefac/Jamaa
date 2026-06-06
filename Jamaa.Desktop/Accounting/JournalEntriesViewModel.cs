using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class JournalEntriesViewModel : ObservableObject, IApplicationModule
{
    private readonly IReadOnlyList<JournalEntryListItemViewModel> _allEntries;
    [ObservableProperty] private string _activeFilterLabel = "All Accounts";

    [ObservableProperty] private bool _expenseAccountsOnly;
    [ObservableProperty] private IReadOnlyList<JournalEntryListItemViewModel> _visibleEntries = [];

    public JournalEntriesViewModel()
    {
        _allEntries = CreatePreviewEntries();
        ApplyNavigationFilter(JournalEntriesNavigationRequest.AllAccounts());
    }

    public Guid Id => Guid.Parse("a1b2c3d4-e5f6-47a8-b9c0-d1e2f3a4b5c6");
    public string Title => "Journal Entries";
    public object? HeaderContent => null;

    /// <summary>
    ///     Integration: Applies navigation context and refreshes the list with the requested account filter.
    /// </summary>
    public void ApplyNavigationFilter(JournalEntriesNavigationRequest request)
    {
        ExpenseAccountsOnly = request.ExpenseAccountsOnly;

        var filteredEntries = _allEntries.AsEnumerable();
        if (request.ExpenseAccountsOnly) filteredEntries = filteredEntries.Where(entry => entry.IsExpenseAccount);

        if (!string.IsNullOrWhiteSpace(request.AccountName))
            filteredEntries = filteredEntries.Where(entry =>
                string.Equals(entry.Account, request.AccountName, StringComparison.OrdinalIgnoreCase));

        VisibleEntries = filteredEntries.ToList();
        ActiveFilterLabel = ResolveFilterLabel(request);
    }

    // Operation: builds the active-filter label shown in the header based on navigation context.
    private static string ResolveFilterLabel(JournalEntriesNavigationRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.AccountName))
        {
            var prefix = request.ExpenseAccountsOnly ? "Expense - " : string.Empty;
            return $"{prefix}{request.AccountName}";
        }

        return request.ExpenseAccountsOnly ? "Expense Accounts" : "All Accounts";
    }

    [RelayCommand]
    private void ShowAllEntries()
    {
        ApplyNavigationFilter(JournalEntriesNavigationRequest.AllAccounts());
    }

    /// <summary>
    ///     Operation: Seeds lightweight preview data until query-backed journal entries are wired in.
    /// </summary>
    private static IReadOnlyList<JournalEntryListItemViewModel> CreatePreviewEntries()
    {
        return
        [
            new("2026-04-02", "JE-1001", "Office Supplies", "Expense", 1450m, true),
            new("2026-04-05", "JE-1002", "Cash", "Asset", 1450m, false),
            new("2026-04-08", "JE-1003", "Utilities", "Expense", 620m, true),
            new("2026-04-11", "JE-1004", "Accounts Payable", "Liability", 620m, false),
            new("2026-04-14", "JE-1005", "Rent Expense", "Expense", 2500m, true)
        ];
    }
}