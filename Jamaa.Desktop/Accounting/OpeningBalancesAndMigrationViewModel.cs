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
    private ObservableCollection<OpeningBalanceItemViewModel> _accounts = [];

    private string _currencySymbol = string.Empty;
    private int _decimalPrecision = 2;
    private string _thousandSeparator = ",";

    private string _fiscalYearId = string.Empty;
    private string _accountingPeriodId = string.Empty;
    private bool _isLocked = true;

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
        // Update the leaf account that was just saved
        var leaf = FlattenVms(Accounts).FirstOrDefault(a => a.Id == balance.AccountId);
        if (leaf != null)
        {
            leaf.OpeningBalance = balance.OpeningBalance;
            leaf.IsLocked = balance.OpeningBalance != 0 || _isLocked;
            leaf.ForceFormatOpeningBalance();
        }

        RecomputeParentBalances();
    }

    private void OnOpeningBalanceSaved(OpeningBalanceItemViewModel _)
    {
        RecomputeParentBalances();
    }

    // Integration: loads all accounts (with hierarchy), fiscal calendar, and opening balances.
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
                _thousandSeparator = settings.ThousandSeparator;
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
                _isLocked = migrationPeriod.IsLocked;
            }
            else
            {
                _fiscalYearId = string.Empty;
                _accountingPeriodId = string.Empty;
                _isLocked = true;
            }

            var roots = await BuildOpeningBalanceTreeAsync(session.Organisation.Id, chartOfAccounts.Accounts);

            Accounts.Clear();
            foreach (var root in roots)
                Accounts.Add(root);
        }
        catch (Exception)
        {
            // Silently fail or log in a real app
        }
    }

    // Integration: builds the full account tree as OpeningBalanceItemViewModels and loads leaf balances.
    private async Task<List<OpeningBalanceItemViewModel>> BuildOpeningBalanceTreeAsync(
        string organisationId,
        IEnumerable<Jamaa.Application.Accounting.Models.AccountData> accounts)
    {
        var accountList = accounts.ToList();

        // First pass: create a VM for every account.
        var vmLookup = accountList.ToDictionary(
            a => a.Id,
            a => new OpeningBalanceItemViewModel(_accountingFacade, _userSessionService, _notificationService)
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                TypeDisplay = a.Type.ToString(),
                CurrencySymbol = _currencySymbol,
                DecimalPrecision = _decimalPrecision,
                ThousandSeparator = _thousandSeparator,
                FiscalYearId = _fiscalYearId,
                AccountingPeriodId = _accountingPeriodId,
            });

        foreach (var vm in vmLookup.Values)
            vm.OpeningBalanceSaved += OnOpeningBalanceSaved;

        // Second pass: wire up parent–child relationships.
        var roots = new List<OpeningBalanceItemViewModel>();
        foreach (var accountData in accountList)
        {
            var vm = vmLookup[accountData.Id];
            if (accountData.ParentId != null && vmLookup.TryGetValue(accountData.ParentId, out var parentVm))
                parentVm.SubAccounts.Add(vm);
            else
                roots.Add(vm);
        }

        // Third pass: mark leaf/parent and fetch opening balances for leaves.
        foreach (var vm in vmLookup.Values)
        {
            vm.IsLeafAccount = vm.SubAccounts.Count == 0;

            if (vm.IsLeafAccount)
            {
                decimal balance = 0;
                if (!string.IsNullOrEmpty(_fiscalYearId) && !string.IsNullOrEmpty(_accountingPeriodId))
                    balance = await _accountingFacade.GetAccountOpeningBalance(
                        organisationId, vm.Id, _fiscalYearId, _accountingPeriodId);

                vm.OpeningBalance = balance;
                vm.IsLocked = balance != 0 || _isLocked;
            }
            else
            {
                // Parent accounts are always read-only; balance is derived from children.
                vm.IsLocked = true;
            }
        }

        // Fourth pass: compute aggregated balances for parent accounts (bottom-up).
        RecomputeParentBalances(roots);

        return roots;
    }

    private void RecomputeParentBalances()
    {
        RecomputeParentBalances(Accounts);
    }

    private static void RecomputeParentBalances(IEnumerable<OpeningBalanceItemViewModel> roots)
    {
        foreach (var root in roots)
            ComputeParentBalance(root);
    }

    // Operation: recursively computes and sets the aggregated opening balance for a parent node.
    private static decimal ComputeParentBalance(OpeningBalanceItemViewModel node)
    {
        if (node.IsLeafAccount) return node.OpeningBalance;

        var total = node.SubAccounts.Sum(child => ComputeParentBalance(child));
        node.OpeningBalance = total;
        return total;
    }

    // Operation: flattens the hierarchical VM tree into a flat sequence.
    private static IEnumerable<OpeningBalanceItemViewModel> FlattenVms(
        IEnumerable<OpeningBalanceItemViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in FlattenVms(node.SubAccounts))
                yield return child;
        }
    }

    public Guid Id => Guid.Parse("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e");
    public string Title => "Opening Balances & Migration";
    public object? HeaderContent => null;
}
