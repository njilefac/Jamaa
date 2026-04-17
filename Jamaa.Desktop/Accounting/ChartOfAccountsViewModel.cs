using System;
using System.Collections.ObjectModel;
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
    private string _pageTitle = "Chat of Accounts Manager (COA)";

    [ObservableProperty]
    private ObservableCollection<AccountItemViewModel> _accounts = [];

    [ObservableProperty]
    private AccountType? _selectedAccountType;

    [ObservableProperty]
    private AccountItemViewModel? _selectedParentAccount;

    [ObservableProperty]
    private string _accountCode = string.Empty;

    public AccountType[] AccountTypes { get; } = Enum.GetValues<AccountType>();

    public ChartOfAccountsViewModel()
    {
        // Sample data for initial display
        LoadSampleData();
    }

    private void LoadSampleData()
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
        // Logic for adding account would go here. For now, it's a stub as per IOSP
        PerformAddAccount();
    }

    private void PerformAddAccount()
    {
        // This is an operation that adds the account.
        // In a real app, this would call a service or send a command.
    }
}