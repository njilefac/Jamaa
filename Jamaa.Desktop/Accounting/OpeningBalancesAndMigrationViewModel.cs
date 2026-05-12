using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Jamaa.Application.Accounting;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Services.Navigation.Interfaces;
using Jamaa.Desktop.Services.Notifications;
using Jamaa.Desktop.Shared;

namespace Jamaa.Desktop.Accounting;

public partial class OpeningBalancesAndMigrationViewModel : ObservableObject, IApplicationModule, IRouteableViewModel
{
    private readonly IAccountingFacade _accountingFacade;
    private readonly IUserSessionService _userSessionService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<OpeningBalanceItemViewModel> _leafAccounts = [];

    private string _currencySymbol = string.Empty;
    private int _decimalPrecision = 2;

    private string _fiscalYearId = string.Empty;
    private string _accountingPeriodId = string.Empty;

    public OpeningBalancesAndMigrationViewModel(
        IAccountingFacade accountingFacade,
        IUserSessionService userSessionService,
        INotificationService notificationService)
    {
        _accountingFacade = accountingFacade;
        _userSessionService = userSessionService;
        _notificationService = notificationService;
        _ = LoadAccountsAsync();

        _accountingFacade.AccountOpeningBalanceSet
            .Subscribe(OnAccountOpeningBalanceSet);
    }

    private void OnAccountOpeningBalanceSet(Jamaa.Application.Accounting.Models.AccountingPeriodBalanceData balance)
    {
        var leaf = LeafAccounts.FirstOrDefault(a => a.Id == balance.AccountId);
        if (leaf != null && leaf.OpeningBalance != balance.OpeningBalance)
        {
            leaf.OpeningBalance = balance.OpeningBalance;
        }
    }

    // Integration: loads the chart of accounts and builds the hierarchical tree.
    internal async Task LoadAccountsAsync()
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
            var fiscalCalendar = await _accountingFacade.GetFiscalCalendar(session.Organisation.Id);

            // Select the migration period: first unlocked period in the first unlocked fiscal year.
            var migrationYear = fiscalCalendar.FiscalYears
                .OrderBy(fy => fy.StartDate)
                .FirstOrDefault(fy => !fy.IsLocked);

            var migrationPeriod = migrationYear?.Periods
                .OrderBy(p => p.SequenceNumber)
                .ThenBy(p => p.StartDate)
                .FirstOrDefault(p => !p.IsLocked);

            if (migrationPeriod != null)
            {
                _fiscalYearId = migrationPeriod.FiscalYearId;
                _accountingPeriodId = migrationPeriod.Id;
            }

            var accounts = chartOfAccounts.Accounts;
            var tree = await BuildAccountTreeAsync(accounts);
            var leaves = Flatten(tree).Where(a => a.IsLeafAccount).ToList();

            LeafAccounts.Clear();
            foreach (var leaf in leaves)
            {
                var openingBalanceVm = new OpeningBalanceItemViewModel(_accountingFacade, _userSessionService, _notificationService)
                {
                    Id = leaf.Id,
                    Code = leaf.Code,
                    Name = leaf.Name,
                    TypeDisplay = leaf.TypeDisplay,
                    IsLeafAccount = leaf.IsLeafAccount,
                    CurrencySymbol = _currencySymbol,
                    DecimalPrecision = _decimalPrecision,
                    FiscalYearId = _fiscalYearId,
                    AccountingPeriodId = _accountingPeriodId
                };

                if (!string.IsNullOrEmpty(_fiscalYearId) && !string.IsNullOrEmpty(_accountingPeriodId))
                {
                    openingBalanceVm.OpeningBalance = await _accountingFacade.GetAccountOpeningBalance(
                        session.Organisation.Id, leaf.Id, _fiscalYearId, _accountingPeriodId);
                }

                LeafAccounts.Add(openingBalanceVm);
            }
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
    private async Task<List<AccountItemViewModel>> BuildAccountTreeAsync(
        IEnumerable<Jamaa.Application.Accounting.Models.AccountData> accounts)
    {
        var accountList = accounts.ToList();
        var viewModels = new List<AccountItemViewModel>();

        foreach (var a in accountList)
        {
            var vm = new AccountItemViewModel
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                Description = a.Description,
                Type = a.Type,
                IsActive = a.IsActive
            };
            viewModels.Add(vm);
        }

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