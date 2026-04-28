using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Finances.Values;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class ChartOfAccountsViewModel : ObservableObject, IApplicationModule, IRouteableViewModel
{
    public Guid Id => Guid.Parse("e2d9f6b1-8e4a-4d9c-8f3b-2a3c4d5e6f7a");
    public string Title => "Chart of Accounts";
    public object? HeaderContent => null;

    [ObservableProperty]
    private string _pageTitle = "Chart of Accounts";

    [ObservableProperty]
    private ObservableCollection<AccountItemViewModel> _accounts = [];

    [ObservableProperty]
    private AccountItemViewModel? _selectedAccount;

    [ObservableProperty]
    private AccountType? _selectedAccountType;

    [ObservableProperty]
    private AccountItemViewModel? _selectedParentAccount;

    public ObservableCollection<AccountItemViewModel> FilteredParentAccounts { get; } = [];

    [ObservableProperty]
    private string _accountCode = string.Empty;

    public AccountType[] AccountTypes { get; } = Enum.GetValues<AccountType>();

    [ObservableProperty]
    private HierarchicalTreeDataGridSource<AccountItemViewModel>? _source;

    public ChartOfAccountsViewModel()
    {
        LoadPreviewData();
        RefreshFilteredParentAccounts();
        InitializeSource();
    }

    private void InitializeSource()
    {
        Source = new HierarchicalTreeDataGridSource<AccountItemViewModel>(Accounts)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<AccountItemViewModel>(
                    new TextColumn<AccountItemViewModel, string>("Code", x => x.Code, options: new TextColumnOptions<AccountItemViewModel> { CanUserSortColumn = true }),
                    x => x.SubAccounts),
                new TextColumn<AccountItemViewModel, string>("Name", x => x.Name, options: new TextColumnOptions<AccountItemViewModel> { CanUserSortColumn = true }),
                new TextColumn<AccountItemViewModel, string>("Type", x => x.TypeName, options: new TextColumnOptions<AccountItemViewModel> { CanUserSortColumn = true }),
            }
        };

        var selection = new TreeDataGridRowSelectionModel<AccountItemViewModel>(Source)
        {
            SingleSelect = true
        };

        Source.Selection = selection;

        selection.SelectionChanged += (s, e) =>
        {
            SelectedAccount = selection.SelectedItem;
        };
    }

    partial void OnSelectedAccountChanged(AccountItemViewModel? value)
    {
        RefreshFilteredParentAccounts();
    }
    partial void OnSelectedAccountTypeChanged(AccountType? value) => RefreshFilteredParentAccounts();

    private void RefreshFilteredParentAccounts()
    {
        FilteredParentAccounts.Clear();
        var allAccounts = GetAllAccounts(Accounts);
        foreach (var account in allAccounts)
        {
            if (CanBeParent(account))
            {
                FilteredParentAccounts.Add(account);
            }
        }
    }

    private IEnumerable<AccountItemViewModel> GetAllAccounts(IEnumerable<AccountItemViewModel> roots)
    {
        foreach (var root in roots)
        {
            yield return root;
            foreach (var child in GetAllAccounts(root.SubAccounts))
            {
                yield return child;
            }
        }
    }

    private bool CanBeParent(AccountItemViewModel potentialParent)
    {
        // 1. Must be of the selected type
        if (SelectedAccountType.HasValue && potentialParent.Type != SelectedAccountType.Value)
            return false;

        // 2. Cannot be itself
        if (SelectedAccount != null && potentialParent == SelectedAccount)
            return false;

        // 3. No cycles
        if (SelectedAccount != null && IsDescendantOf(potentialParent, SelectedAccount))
            return false;

        return true;
    }

    private bool IsDescendantOf(AccountItemViewModel node, AccountItemViewModel potentialAncestor)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current == potentialAncestor)
                return true;
            current = current.Parent;
        }
        return false;
    }

    private void LoadPreviewData()
    {
        var asset = new AccountItemViewModel { Name = "Assets", Code = "1000", Type = AccountType.Asset };
        asset.SubAccounts.Add(new AccountItemViewModel { Name = "Cash", Code = "1010", Type = AccountType.Asset, Parent = asset });
        asset.SubAccounts.Add(new AccountItemViewModel { Name = "Bank", Code = "1020", Type = AccountType.Asset, Parent = asset });

        var liability = new AccountItemViewModel { Name = "Liabilities", Code = "2000", Type = AccountType.Liability };
        liability.SubAccounts.Add(new AccountItemViewModel { Name = "Accounts Payable", Code = "2010", Type = AccountType.Liability, Parent = liability });

        var equity = new AccountItemViewModel { Name = "Owner's Equity", Code = "3000", Type = AccountType.OwnersEquity };

        Accounts.Add(asset);
        Accounts.Add(liability);
        Accounts.Add(equity);
    }

    [RelayCommand]
    private void AddAccount()
    {
        PerformAddAccount();
    }

    private void PerformAddAccount()
    {
        // Operation placeholder: command dispatch will be added when account management is wired up.
    }
}