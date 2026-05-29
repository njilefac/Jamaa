using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jamaa.Application.Accounting;
using Jamaa.Application.Users.Services;
using Jamaa.Desktop.Services.Notifications;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Jamaa.Desktop.Accounting;

public partial class OpeningBalanceItemViewModel(
    IAccountingFacade accountingFacade,
    IUserSessionService userSessionService,
    INotificationService notificationService)
    : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _typeDisplay = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyCanExecuteChangedFor(nameof(SaveOpeningBalanceCommand))]
    private bool _isLeafAccount;

    [ObservableProperty] private string _currencySymbol = string.Empty;
    [ObservableProperty] private int _decimalPrecision = 2;
    [ObservableProperty] private decimal _openingBalance;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveOpeningBalanceCommand))]
    private bool _isSaving;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyCanExecuteChangedFor(nameof(SaveOpeningBalanceCommand))]
    private bool _isLocked;

    /// <summary>True for editable leaf accounts that have no saved balance yet.</summary>
    public bool CanEdit => IsLeafAccount && !IsLocked;

    /// <summary>Children of this account in the hierarchy (empty for leaf accounts).</summary>
    public ObservableCollection<OpeningBalanceItemViewModel> SubAccounts { get; } = [];

    public void ForceFormatOpeningBalance()
    {
        OnPropertyChanged(nameof(OpeningBalance));
    }

    [ObservableProperty] private string _fiscalYearId = string.Empty;
    [ObservableProperty] private string _accountingPeriodId = string.Empty;

    [RelayCommand(CanExecute = nameof(CanSaveOpeningBalance))]
    private async Task SaveOpeningBalanceAsync()
    {
        var session = userSessionService.CurrentUserSession;
        if (session?.Organisation?.Id == null) return;

        var orgId = session.Organisation.Id;
        var subject = $"Opening balance for account - {Name}";

        // Integration: sends command, waits for confirmation, then refreshes UI on success
        var isConfirmed = await notificationService.TrackOperationAsync(
            () => accountingFacade.SetAccountOpeningBalance(
                orgId,
                Id,
                FiscalYearId,
                AccountingPeriodId,
                OpeningBalance),
            BuildSaveConfirmationObservable(orgId),
            TimeSpan.FromSeconds(10),
            "Opening Balance",
            "Saved",
            subject,
            inFlight => IsSaving = inFlight);

        if (isConfirmed)
        {
            // Operation: locks account and refreshes textbox display after successful save
            IsLocked = true;
            ForceFormatOpeningBalance();
        }
    }

    // Operation: guard for the save command — only editable leaf accounts without a saved balance can save.
    private bool CanSaveOpeningBalance() => IsLeafAccount && !IsLocked && !IsSaving;

    private IObservable<bool> BuildSaveConfirmationObservable(string organisationId)
    {
        var events = accountingFacade.AccountOpeningBalanceSet
            .Where(x => x.AccountId == Id && x.FiscalYearId == FiscalYearId && x.AccountingPeriodId == AccountingPeriodId)
            .Select(_ => true);

        var readModelPolling = Observable.Interval(TimeSpan.FromMilliseconds(250))
            .StartWith(0L)
            .SelectMany(_ => Observable.FromAsync(() =>
                HasOpeningBalanceAppearedAsync(organisationId)))
            .Where(isPresent => isPresent)
            .Take(1);

        return events.Merge(readModelPolling).Take(1);
    }

    private async Task<bool> HasOpeningBalanceAppearedAsync(string organisationId)
    {
        try
        {
            var balance = await accountingFacade.GetAccountOpeningBalance(
                organisationId,
                Id,
                FiscalYearId,
                AccountingPeriodId);

            return balance == OpeningBalance;
        }
        catch
        {
            return false;
        }
    }
}
