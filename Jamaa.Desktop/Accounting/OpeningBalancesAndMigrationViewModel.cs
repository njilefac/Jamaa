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
    private ObservableCollection<AccountItemViewModel> _leafAccounts = [];

    private string _currencySymbol = string.Empty;
    private int _decimalPrecision = 2;

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

        try
        {
            var settings = await _accountingFacade.GetAccountingSettings(session.Organisation.Id);
            if (settings != null)
            {
                _decimalPrecision = settings.DecimalPrecision;
                _currencySymbol = settings.AvailableCurrencies
                    .FirstOrDefault(c => c.CurrencyCode == settings.BaseCurrency)?
                    .CurrencySymbol ?? string.Empty;
            }

            var chartOfAccounts = await _accountingFacade.GetChartOfAccounts(session.Organisation.Id);
            var accounts = chartOfAccounts.Accounts;
            var tree = BuildAccountTree(accounts);
            var leaves = Flatten(tree).Where(a => a.IsLeafAccount).ToList();

            LeafAccounts.Clear();
            foreach (var leaf in leaves)
                LeafAccounts.Add(leaf);
        }
        catch (Exception)
        {
            // Silently fail or log in a real app
        }
    }

    // Operation: flattens the hierarchical tree into a flat list.
    private IEnumerable<AccountItemViewModel> Flatten(IEnumerable<AccountItemViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.SubAccounts))
                yield return child;
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
                OpeningBalance = 0m,
                CurrencySymbol = _currencySymbol,
                DecimalPrecision = _decimalPrecision
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

    public Guid Id => Guid.Parse("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e");
    public string Title => "Opening Balances & Migration";
    public object? HeaderContent => null;
}