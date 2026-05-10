using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jamaa.Application.Accounting;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class OpeningBalancesAndMigrationViewModel : ObservableObject, IApplicationModule, IRouteableViewModel
{
    private readonly IAccountingFacade _accountingFacade;
    private readonly IUserSessionService _userSessionService;

    [ObservableProperty]
    private ObservableCollection<AccountItemViewModel> _accountTree = [];

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public OpeningBalancesAndMigrationViewModel(
        IAccountingFacade accountingFacade,
        IUserSessionService userSessionService)
    {
        _accountingFacade = accountingFacade;
        _userSessionService = userSessionService;
        LoadAccountsAsync();
    }

    // Integration: loads the chart of accounts and builds the hierarchical tree.
    private async void LoadAccountsAsync()
    {
        var session = _userSessionService.CurrentUserSession;
        if (session?.Organisation?.Id == null) return;

        IsLoading = true;
        try
        {
            var chartOfAccounts = await _accountingFacade.GetChartOfAccounts(session.Organisation.Id);
            var accounts = chartOfAccounts.Accounts;
            var tree = BuildAccountTree(accounts);

            AccountTree.Clear();
            foreach (var root in tree)
                AccountTree.Add(root);

            StatusMessage = "Chart of Accounts loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading accounts: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Operation: builds a hierarchical tree from a flat list of accounts.
    private List<AccountItemViewModel> BuildAccountTree(
        IEnumerable<Jamaa.Application.Accounting.Models.AccountData> accounts)
    {
        var accountList = accounts.ToList();

        var viewModels = accountList.Select(a =>
        {
            var vm = new AccountItemViewModel
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                Description = a.Description,
                Type = a.Type,
                IsActive = a.IsActive,
                OpeningBalance = 0m
            };
            return vm;
        }).ToList();

        var lookup = viewModels.ToDictionary(a => a.Id);
        var roots = new List<AccountItemViewModel>();

        foreach (var accountData in accountList)
        {
            var vm = lookup[accountData.Id];
            if (accountData.ParentId != null && lookup.TryGetValue(accountData.ParentId, out var parentVm))
            {
                vm.Parent = parentVm;
                parentVm.SubAccounts.Add(vm);
            }
            else
            {
                roots.Add(vm);
            }
        }

        return roots;
    }

    // Operation: propagates leaf account balance changes up the tree for parent aggregation.
    [RelayCommand]
    public void OnLeafBalanceChanged(AccountItemViewModel account)
    {
        if (!account.IsLeafAccount)
            return;

        account.PropagateBalanceChange();
        StatusMessage = $"Opening balance updated for {account.Name}. Parent accounts aggregate automatically.";
    }

    public Guid Id => Guid.Parse("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e");
    public string Title => "Opening Balances & Migration";
    public object? HeaderContent => null;
}